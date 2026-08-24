using System.Text.Json;
using DungeonApp.Domain.Campaigns;
using DungeonApp.Domain.Campaigns.Events;
using DungeonApp.Domain.Campaigns.Modules;
using DungeonApp.Domain.Campaigns.Modules.Clock;

namespace DungeonApp.Infrastructure.Campaigns.Persistence;

public sealed class ClockPersistenceAdapter : ICampaignModulePersistenceAdapter, ICampaignEventPersistenceAdapter
{
    public const string WorldTimeAdvancedEventType = "core.clock.world-time-advanced.v1";

    public ModuleId ModuleId => ClockModule.Id;

    public string EventType => WorldTimeAdvancedEventType;

    public bool CanWrite(ICampaignModule module) => module is ClockModule;

    public JsonElement WriteState(ICampaignModule module, JsonSerializerOptions serializerOptions)
    {
        var clock = module as ClockModule
            ?? throw new ArgumentException("Clock persistence adapter can only write a clock module.", nameof(module));

        return JsonSerializer.SerializeToElement(new ClockState(clock.CurrentTime.Elapsed.Ticks), serializerOptions);
    }

    public ICampaignModule ReadState(int stateVersion, JsonElement state, JsonSerializerOptions serializerOptions)
    {
        if (stateVersion != 1)
        {
            throw new InvalidDataException($"Clock module state version '{stateVersion}' is not supported.");
        }

        var clockState = state.Deserialize<ClockState>(serializerOptions)
            ?? throw new InvalidDataException("Clock module state is missing.");

        return new ClockModule(new CampaignTime(TimeSpan.FromTicks(clockState.CurrentTimeTicks)));
    }

    public bool CanWrite(ICampaignEventPayload payload) => payload is WorldTimeAdvanced;

    public JsonElement WritePayload(ICampaignEventPayload payload, JsonSerializerOptions serializerOptions)
    {
        var worldTimeAdvanced = payload as WorldTimeAdvanced
            ?? throw new ArgumentException("Clock persistence adapter can only write world-time events.", nameof(payload));

        return JsonSerializer.SerializeToElement(
            new WorldTimeAdvancedState(
                worldTimeAdvanced.From.Elapsed.Ticks,
                worldTimeAdvanced.To.Elapsed.Ticks,
                worldTimeAdvanced.Elapsed.Ticks,
                worldTimeAdvanced.Reason),
            serializerOptions);
    }

    public ICampaignEventPayload ReadPayload(JsonElement payload, JsonSerializerOptions serializerOptions)
    {
        var eventState = payload.Deserialize<WorldTimeAdvancedState>(serializerOptions)
            ?? throw new InvalidDataException("World-time event payload is missing.");

        if (string.IsNullOrWhiteSpace(eventState.Reason))
        {
            throw new InvalidDataException("World-time event reason is missing.");
        }

        var from = new CampaignTime(TimeSpan.FromTicks(eventState.FromTicks));
        var elapsed = TimeSpan.FromTicks(eventState.ElapsedTicks);
        var to = new CampaignTime(TimeSpan.FromTicks(eventState.ToTicks));

        if (elapsed <= TimeSpan.Zero || from.AdvanceBy(elapsed) != to)
        {
            throw new InvalidDataException("World-time event payload is inconsistent.");
        }

        return new WorldTimeAdvanced(from, to, elapsed, eventState.Reason);
    }

    private sealed record ClockState(long CurrentTimeTicks);

    private sealed record WorldTimeAdvancedState(long FromTicks, long ToTicks, long ElapsedTicks, string Reason);
}
