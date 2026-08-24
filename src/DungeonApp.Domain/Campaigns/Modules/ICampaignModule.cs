using DungeonApp.Domain.Campaigns.Events;

namespace DungeonApp.Domain.Campaigns.Modules;

/// <summary>
/// Autonomous part of a campaign. A module may only affect the campaign by publishing events
/// through the context supplied by the host.
/// </summary>
public interface ICampaignModule
{
    CampaignModuleDescriptor Descriptor { get; }

    /// <summary>
    /// Creates an isolated working copy. The host swaps all copies into the campaign only after
    /// the whole command and resulting event cascade succeeds.
    /// </summary>
    ICampaignModule CreateWorkingCopy();

    void Handle(CampaignCommand command, CampaignModuleContext context);

    void ReactTo(CampaignEvent campaignEvent, CampaignModuleContext context);
}
