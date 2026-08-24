using System.Text.Json;
using DungeonApp.Application.Campaigns;
using DungeonApp.Domain.Campaigns;
using DungeonApp.Domain.Campaigns.Events;
using DungeonApp.Domain.Campaigns.Modules;
using DungeonApp.Domain.Campaigns.Modules.Clock;
using DungeonApp.Domain.Campaigns.Modules.Scheduler;

namespace DungeonApp.Infrastructure.Campaigns.Persistence;

/// <summary>
/// Local JSON implementation of the campaign repository. Persistence adapters are explicit,
/// versioned extension points for module state and event payloads.
/// </summary>
public sealed class JsonCampaignRepository : ICampaignRepository
{
    private readonly string _directoryPath;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private readonly IReadOnlyDictionary<ModuleId, ICampaignModulePersistenceAdapter> _moduleAdapters;
    private readonly IReadOnlyDictionary<string, ICampaignEventPersistenceAdapter> _eventAdapters;
    private readonly IReadOnlyDictionary<ModuleId, ICampaignModuleFactory> _moduleFactories;

    public JsonCampaignRepository(
        string directoryPath,
        IEnumerable<ICampaignModulePersistenceAdapter>? moduleAdapters = null,
        IEnumerable<ICampaignEventPersistenceAdapter>? eventAdapters = null,
        IEnumerable<ICampaignModuleFactory>? moduleFactories = null)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Campaign directory path cannot be empty.", nameof(directoryPath));
        }

        _directoryPath = directoryPath;

        var defaultAdapter = new ClockPersistenceAdapter();
        var defaultSchedulerModuleAdapter = new SchedulerModulePersistenceAdapter();
        var defaultConfigurationAdapter = new CampaignConfigurationPersistenceAdapter();
        var defaultScheduledEventAdapter = new WorldEventScheduledPersistenceAdapter();
        var defaultDueEventAdapter = new ScheduledWorldEventDuePersistenceAdapter();
        var defaultFactory = new ClockModuleFactory();
        var defaultSchedulerFactory = new SchedulerModuleFactory();
        var configuredModuleAdapters = moduleAdapters?.ToList() ?? [defaultAdapter, defaultSchedulerModuleAdapter];
        var configuredEventAdapters = eventAdapters?.ToList() ??
            [defaultAdapter, defaultConfigurationAdapter, defaultScheduledEventAdapter, defaultDueEventAdapter];
        var configuredFactories = moduleFactories?.ToList() ?? [defaultFactory, defaultSchedulerFactory];

        _moduleAdapters = configuredModuleAdapters.ToDictionary(adapter => adapter.ModuleId);
        _eventAdapters = configuredEventAdapters.ToDictionary(adapter => adapter.EventType, StringComparer.Ordinal);
        _moduleFactories = configuredFactories.ToDictionary(factory => factory.ModuleId);
    }

    public async Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_directoryPath))
        {
            return [];
        }

        var summaries = new List<CampaignSummary>();

        foreach (var path in Directory.EnumerateFiles(_directoryPath, "*.campaign.json", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var stream = File.OpenRead(path);
            var document = await JsonSerializer.DeserializeAsync<CampaignDocument>(stream, _serializerOptions, cancellationToken)
                ?? throw new InvalidDataException($"Campaign document '{path}' is empty.");
            summaries.Add(new CampaignSummary(
                document.Id,
                document.Name,
                document.Modules.Select(module => module.Key).ToList().AsReadOnly()));
        }

        return summaries.OrderBy(summary => summary.Name, StringComparer.CurrentCultureIgnoreCase).ToList().AsReadOnly();
    }

    public async Task<Campaign?> FindByIdAsync(CampaignId id, CancellationToken cancellationToken = default)
    {
        var path = GetPath(id);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<CampaignDocument>(stream, _serializerOptions, cancellationToken)
            ?? throw new InvalidDataException("Campaign document is empty.");

        if (document.Id != id.Value)
        {
            throw new InvalidDataException("Campaign document id does not match its file name.");
        }

        var modules = document.Modules.Select(ReadModule).ToList();
        var history = document.History.Select(ReadEvent).ToList();

        return Campaign.Restore(new CampaignId(document.Id), document.Name, modules, history, _moduleFactories.Values);
    }

    public async Task SaveAsync(Campaign campaign, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        Directory.CreateDirectory(_directoryPath);

        var document = new CampaignDocument(
            campaign.Id.Value,
            campaign.Name,
            campaign.Modules.Select(WriteModule).ToList(),
            campaign.History.Select(WriteEvent).ToList());

        var destinationPath = GetPath(campaign.Id);
        var temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, document, _serializerOptions, cancellationToken);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private ModuleStateDocument WriteModule(ICampaignModule module)
    {
        var adapter = _moduleAdapters.GetValueOrDefault(module.Descriptor.Id)
            ?? throw new InvalidOperationException($"No persistence adapter is registered for module '{module.Descriptor.Id}'.");

        if (!adapter.CanWrite(module))
        {
            throw new InvalidOperationException($"Persistence adapter cannot write module '{module.Descriptor.Id}'.");
        }

        return new ModuleStateDocument(module.Descriptor.Id.Value, module.Descriptor.StateVersion, adapter.WriteState(module, _serializerOptions));
    }

    private EventDocument WriteEvent(CampaignEvent campaignEvent)
    {
        var adapter = _eventAdapters.Values.SingleOrDefault(candidate => candidate.CanWrite(campaignEvent.Payload))
            ?? throw new InvalidOperationException($"No persistence adapter is registered for event '{campaignEvent.Payload.GetType().Name}'.");

        return new EventDocument(
            adapter.EventType,
            campaignEvent.SequenceNumber,
            JsonSerializer.SerializeToElement(new EventEnvelopeDocument(
                campaignEvent.EventId,
                campaignEvent.SourceModule.Value,
                campaignEvent.CorrelationId,
                campaignEvent.CausationEventId,
                campaignEvent.OccurredAt.Elapsed.Ticks,
                adapter.WritePayload(campaignEvent.Payload, _serializerOptions)), _serializerOptions));
    }

    private ICampaignModule ReadModule(ModuleStateDocument document)
    {
        var moduleId = new ModuleId(document.Key);
        var adapter = _moduleAdapters.GetValueOrDefault(moduleId)
            ?? throw new InvalidDataException($"No persistence adapter is registered for module '{moduleId}'.");

        return adapter.ReadState(document.Version, document.Data, _serializerOptions);
    }

    private CampaignEvent ReadEvent(EventDocument document)
    {
        var adapter = _eventAdapters.GetValueOrDefault(document.Key)
            ?? throw new InvalidDataException($"No persistence adapter is registered for event type '{document.Key}'.");
        var envelope = document.Data.Deserialize<EventEnvelopeDocument>(_serializerOptions)
            ?? throw new InvalidDataException("Campaign event envelope is missing.");

        return new CampaignEvent(
            envelope.EventId,
            document.SequenceNumber,
            new ModuleId(envelope.SourceModule),
            envelope.CorrelationId,
            envelope.CausationEventId,
            new CampaignTime(TimeSpan.FromTicks(envelope.OccurredAtTicks)),
            adapter.ReadPayload(envelope.Payload, _serializerOptions));
    }

    private string GetPath(CampaignId id) => Path.Combine(_directoryPath, $"{id.Value:N}.campaign.json");

    private sealed record CampaignDocument(
        Guid Id,
        string Name,
        List<ModuleStateDocument> Modules,
        List<EventDocument> History);

    private sealed record ModuleStateDocument(string Key, int Version, JsonElement Data);

    private sealed record EventDocument(string Key, long SequenceNumber, JsonElement Data);

    private sealed record EventEnvelopeDocument(
        Guid EventId,
        string SourceModule,
        Guid CorrelationId,
        Guid? CausationEventId,
        long OccurredAtTicks,
        JsonElement Payload);
}
