namespace DungeonApp.Domain.Campaigns.Modules;

/// <summary>
/// Persisted identity and state-contract version of an enabled module.
/// </summary>
public sealed record CampaignModuleDescriptor
{
    public CampaignModuleDescriptor(ModuleId id, int stateVersion)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (stateVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(stateVersion), "State version must be at least one.");
        }

        Id = id;
        StateVersion = stateVersion;
    }

    public ModuleId Id { get; }

    public int StateVersion { get; }
}
