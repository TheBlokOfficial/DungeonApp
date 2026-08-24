namespace DungeonApp.Domain.Campaigns;

/// <summary>
/// An explicit request made against one enabled campaign module.
/// Commands are not persisted as campaign history; the resulting domain events are.
/// </summary>
public abstract record CampaignCommand
{
    protected CampaignCommand(Guid commandId, ModuleId targetModule, string reason)
    {
        if (commandId == Guid.Empty)
        {
            throw new ArgumentException("Command id cannot be empty.", nameof(commandId));
        }

        ArgumentNullException.ThrowIfNull(targetModule);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A command requires a reason.", nameof(reason));
        }

        CommandId = commandId;
        TargetModule = targetModule;
        Reason = reason.Trim();
    }

    public Guid CommandId { get; }

    public ModuleId TargetModule { get; }

    public string Reason { get; }
}
