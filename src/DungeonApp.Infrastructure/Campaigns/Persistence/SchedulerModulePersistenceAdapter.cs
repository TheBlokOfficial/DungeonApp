using System.Text.Json;
using DungeonApp.Domain.Campaigns;
using DungeonApp.Domain.Campaigns.Modules;
using DungeonApp.Domain.Campaigns.Modules.Scheduler;

namespace DungeonApp.Infrastructure.Campaigns.Persistence;

public sealed class SchedulerModulePersistenceAdapter : ICampaignModulePersistenceAdapter
{
    public ModuleId ModuleId => SchedulerModule.Id;

    public bool CanWrite(ICampaignModule module) => module is SchedulerModule;

    public JsonElement WriteState(ICampaignModule module, JsonSerializerOptions serializerOptions)
    {
        var scheduler = module as SchedulerModule
            ?? throw new ArgumentException("Scheduler persistence adapter can only write a scheduler module.", nameof(module));

        return JsonSerializer.SerializeToElement(
            scheduler.ScheduledEvents.Select(SchedulerEventPersistence.ToState).ToList(),
            serializerOptions);
    }

    public ICampaignModule ReadState(int stateVersion, JsonElement state, JsonSerializerOptions serializerOptions)
    {
        if (stateVersion != 1)
        {
            throw new InvalidDataException($"Scheduler module state version '{stateVersion}' is not supported.");
        }

        var scheduledEventStates = state.Deserialize<List<SchedulerEventPersistence.ScheduledWorldEventState>>(serializerOptions)
            ?? throw new InvalidDataException("Scheduler module state is missing.");

        return new SchedulerModule(scheduledEventStates.Select(SchedulerEventPersistence.FromState));
    }
}
