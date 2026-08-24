namespace DungeonApp.Domain.Campaigns.Modules;

/// <summary>
/// Optional capability used by the campaign host to timestamp core configuration events.
/// </summary>
public interface ICampaignTimeSource
{
    CampaignTime CurrentTime { get; }
}
