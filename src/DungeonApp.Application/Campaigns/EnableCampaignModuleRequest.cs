namespace DungeonApp.Application.Campaigns;

public sealed record EnableCampaignModuleRequest(Guid CampaignId, string ModuleId, string Reason);
