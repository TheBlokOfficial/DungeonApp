using System.Text.Json;
using DungeonApp.Domain.Campaigns;
using DungeonApp.Domain.Campaigns.Modules;

namespace DungeonApp.Infrastructure.Campaigns.Persistence;

/// <summary>
/// Infrastructure-owned translator for one module's private state.
/// Adding a module does not require the campaign host to learn its serialization details.
/// </summary>
public interface ICampaignModulePersistenceAdapter
{
    ModuleId ModuleId { get; }

    bool CanWrite(ICampaignModule module);

    JsonElement WriteState(ICampaignModule module, JsonSerializerOptions serializerOptions);

    ICampaignModule ReadState(int stateVersion, JsonElement state, JsonSerializerOptions serializerOptions);
}
