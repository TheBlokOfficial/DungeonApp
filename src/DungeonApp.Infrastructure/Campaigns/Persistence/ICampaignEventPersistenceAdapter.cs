using System.Text.Json;
using DungeonApp.Domain.Campaigns.Events;

namespace DungeonApp.Infrastructure.Campaigns.Persistence;

/// <summary>
/// Infrastructure-owned translator for a versioned event payload.
/// </summary>
public interface ICampaignEventPersistenceAdapter
{
    string EventType { get; }

    bool CanWrite(ICampaignEventPayload payload);

    JsonElement WritePayload(ICampaignEventPayload payload, JsonSerializerOptions serializerOptions);

    ICampaignEventPayload ReadPayload(JsonElement payload, JsonSerializerOptions serializerOptions);
}
