using System.Text.Json;
using DungeonApp.Domain.Campaigns.Events;

namespace DungeonApp.Infrastructure.Campaigns.Persistence;

/// <summary>
/// Persists campaign-core configuration events independently of individual modules.
/// </summary>
public sealed class CampaignConfigurationPersistenceAdapter : ICampaignEventPersistenceAdapter
{
    public const string ModuleEnabledEventType = "core.campaign.module-enabled.v1";

    public string EventType => ModuleEnabledEventType;

    public bool CanWrite(ICampaignEventPayload payload) => payload is CampaignModuleEnabled;

    public JsonElement WritePayload(ICampaignEventPayload payload, JsonSerializerOptions serializerOptions)
    {
        var moduleEnabled = payload as CampaignModuleEnabled
            ?? throw new ArgumentException("Configuration adapter can only write module-enabled events.", nameof(payload));

        return JsonSerializer.SerializeToElement(
            new ModuleEnabledState(moduleEnabled.ModuleId.Value, moduleEnabled.StateVersion, moduleEnabled.Reason),
            serializerOptions);
    }

    public ICampaignEventPayload ReadPayload(JsonElement payload, JsonSerializerOptions serializerOptions)
    {
        var state = payload.Deserialize<ModuleEnabledState>(serializerOptions)
            ?? throw new InvalidDataException("Module-enabled event payload is missing.");

        if (state.StateVersion < 1 || string.IsNullOrWhiteSpace(state.Reason))
        {
            throw new InvalidDataException("Module-enabled event payload is invalid.");
        }

        return new CampaignModuleEnabled(new Domain.Campaigns.ModuleId(state.ModuleId), state.StateVersion, state.Reason);
    }

    private sealed record ModuleEnabledState(string ModuleId, int StateVersion, string Reason);
}
