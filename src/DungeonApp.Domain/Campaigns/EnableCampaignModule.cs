namespace DungeonApp.Domain.Campaigns;

/// <summary>
/// Core command that changes campaign configuration. It is handled by the campaign host,
/// rather than by an already-enabled module.
/// </summary>
public sealed record EnableCampaignModule : CampaignCommand
{
    public EnableCampaignModule(Guid commandId, ModuleId moduleId, string reason)
        : base(commandId, Campaign.CoreModuleId, reason)
    {
        ArgumentNullException.ThrowIfNull(moduleId);

        if (moduleId == Campaign.CoreModuleId)
        {
            throw new ArgumentException("The campaign core is always enabled.", nameof(moduleId));
        }

        ModuleId = moduleId;
    }

    public ModuleId ModuleId { get; }
}
