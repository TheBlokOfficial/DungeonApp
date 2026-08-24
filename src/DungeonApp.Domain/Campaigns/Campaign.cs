using DungeonApp.Domain.Campaigns.Events;
using DungeonApp.Domain.Campaigns.Modules;

namespace DungeonApp.Domain.Campaigns;

/// <summary>
/// Aggregate root and local event router for a campaign.
/// It deliberately is not a global event bus: one command is handled synchronously and either
/// all affected module copies and history entries are committed together, or none are.
/// </summary>
public sealed class Campaign
{
    private const int MaximumEventsPerCommand = 1_000;
    private List<ICampaignModule> _modules;
    private readonly IReadOnlyDictionary<ModuleId, ICampaignModuleFactory> _moduleFactories;
    private readonly List<CampaignEvent> _history = [];
    private long _nextSequenceNumber = 1;

    public Campaign(
        CampaignId id,
        string name,
        IEnumerable<ICampaignModule>? modules = null,
        IEnumerable<ICampaignModuleFactory>? moduleFactories = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Campaign name cannot be empty.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
        _modules = modules?.ToList() ?? [];
        _moduleFactories = (moduleFactories ?? []).ToDictionary(factory => factory.ModuleId);

        ValidateModules(_modules);
    }

    public CampaignId Id { get; }

    public static ModuleId CoreModuleId { get; } = new("core.campaign");

    public string Name { get; }

    /// <summary>
    /// Enabled module instances. Consumers should treat them as read-only state;
    /// command handling remains exclusively owned by <see cref="Execute"/>.
    /// </summary>
    public IReadOnlyList<ICampaignModule> Modules => _modules.AsReadOnly();

    public IReadOnlyList<CampaignModuleDescriptor> EnabledModules => _modules
        .Select(module => module.Descriptor)
        .ToList()
        .AsReadOnly();

    public IReadOnlyList<CampaignEvent> History => _history.AsReadOnly();

    /// <summary>
    /// Rehydrates a campaign whose module state and history have already been validated by its
    /// persistence adapter. The core still protects sequence and event-identity invariants.
    /// </summary>
    public static Campaign Restore(
        CampaignId id,
        string name,
        IEnumerable<ICampaignModule> modules,
        IEnumerable<CampaignEvent> history,
        IEnumerable<ICampaignModuleFactory>? moduleFactories = null)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(history);

        var campaign = new Campaign(id, name, modules, moduleFactories);
        var restoredHistory = history.ToList();
        ValidateHistory(restoredHistory);

        campaign._history.AddRange(restoredHistory);
        campaign._nextSequenceNumber = restoredHistory.Count + 1;

        return campaign;
    }

    public TModule GetModule<TModule>(ModuleId id)
        where TModule : class, ICampaignModule
    {
        ArgumentNullException.ThrowIfNull(id);

        return _modules.SingleOrDefault(module => module.Descriptor.Id == id) as TModule
            ?? throw new CampaignModuleNotEnabledException(id);
    }

    /// <summary>
    /// Routes one command and every event it causes. The returned events are exactly the new,
    /// committed part of history.
    /// </summary>
    public IReadOnlyList<CampaignEvent> Execute(CampaignCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workingModules = _modules.Select(module => module.CreateWorkingCopy()).ToList();
        ValidateModules(workingModules);

        var pendingEvents = new Queue<CampaignModuleContext.PendingCampaignEvent>();
        if (command is EnableCampaignModule enableModule)
        {
            EnableModule(enableModule, workingModules, pendingEvents);
        }
        else
        {
            var target = workingModules.SingleOrDefault(module => module.Descriptor.Id == command.TargetModule)
                ?? throw new CampaignModuleNotEnabledException(command.TargetModule);
            target.Handle(command, CreateContext(
                pendingEvents,
                target.Descriptor.Id,
                command.CommandId,
                null,
                GetCurrentTime(workingModules)));
        }

        var committedEvents = new List<CampaignEvent>();

        while (pendingEvents.TryDequeue(out var pendingEvent))
        {
            if (committedEvents.Count == MaximumEventsPerCommand)
            {
                throw new InvalidOperationException($"A command cannot produce more than {MaximumEventsPerCommand} events.");
            }

            var campaignEvent = new CampaignEvent(
                pendingEvent.EventId,
                _nextSequenceNumber + committedEvents.Count,
                pendingEvent.SourceModule,
                pendingEvent.CorrelationId,
                pendingEvent.CausationEventId,
                pendingEvent.OccurredAt,
                pendingEvent.Payload);

            committedEvents.Add(campaignEvent);

            foreach (var module in workingModules)
            {
                module.ReactTo(
                    campaignEvent,
                    CreateContext(
                        pendingEvents,
                        module.Descriptor.Id,
                        command.CommandId,
                        campaignEvent.EventId,
                        GetCurrentTime(workingModules)));
            }
        }

        // No externally visible state changes are made before all handlers have finished.
        _modules = workingModules;
        _history.AddRange(committedEvents);
        _nextSequenceNumber += committedEvents.Count;

        return committedEvents.AsReadOnly();
    }

    private static CampaignModuleContext CreateContext(
        Queue<CampaignModuleContext.PendingCampaignEvent> pendingEvents,
        ModuleId sourceModule,
        Guid correlationId,
        Guid? causationEventId,
        CampaignTime currentTime) => new(pendingEvents, sourceModule, correlationId, causationEventId, currentTime);

    private void EnableModule(
        EnableCampaignModule command,
        ICollection<ICampaignModule> workingModules,
        Queue<CampaignModuleContext.PendingCampaignEvent> pendingEvents)
    {
        if (workingModules.Any(module => module.Descriptor.Id == command.ModuleId))
        {
            throw new CampaignModuleAlreadyEnabledException(command.ModuleId);
        }

        var factory = _moduleFactories.GetValueOrDefault(command.ModuleId)
            ?? throw new CampaignModuleUnavailableException(command.ModuleId);
        var module = factory.Create();

        if (module.Descriptor.Id != command.ModuleId)
        {
            throw new InvalidOperationException($"Factory for '{command.ModuleId}' created module '{module.Descriptor.Id}'.");
        }

        workingModules.Add(module);
        var occurredAt = GetCurrentTime(workingModules);
        ValidateModuleDependencies(workingModules);
        CreateContext(pendingEvents, CoreModuleId, command.CommandId, null, occurredAt).Publish(
            new CampaignModuleEnabled(module.Descriptor.Id, module.Descriptor.StateVersion, command.Reason),
            occurredAt);
    }

    private static CampaignTime GetCurrentTime(IEnumerable<ICampaignModule> modules)
    {
        var timeSources = modules.OfType<ICampaignTimeSource>().ToList();

        return timeSources.Count switch
        {
            0 => CampaignTime.Start,
            1 => timeSources[0].CurrentTime,
            _ => throw new InvalidOperationException("Only one campaign time source can be enabled.")
        };
    }

    private static void ValidateModules(IReadOnlyCollection<ICampaignModule> modules)
    {
        var duplicate = modules
            .GroupBy(module => module.Descriptor.Id)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Campaign module '{duplicate.Key}' is enabled more than once.");
        }

        ValidateModuleDependencies(modules);
    }

    private static void ValidateModuleDependencies(IEnumerable<ICampaignModule> modules)
    {
        var enabledModuleIds = modules.Select(module => module.Descriptor.Id).ToHashSet();

        foreach (var declaration in modules.OfType<ICampaignModuleDependencyDeclaration>())
        {
            foreach (var requiredModuleId in declaration.RequiredModuleIds)
            {
                if (!enabledModuleIds.Contains(requiredModuleId))
                {
                    throw new InvalidOperationException(
                        $"Campaign module requires enabled module '{requiredModuleId}'.");
                }
            }
        }
    }

    private static void ValidateHistory(IReadOnlyList<CampaignEvent> history)
    {
        var duplicateEvent = history.GroupBy(campaignEvent => campaignEvent.EventId).FirstOrDefault(group => group.Count() > 1);
        if (duplicateEvent is not null)
        {
            throw new InvalidOperationException($"Campaign history contains duplicate event id '{duplicateEvent.Key}'.");
        }

        for (var index = 0; index < history.Count; index++)
        {
            if (history[index].SequenceNumber != index + 1)
            {
                throw new InvalidOperationException("Campaign history must have consecutive sequence numbers starting at one.");
            }
        }
    }
}
