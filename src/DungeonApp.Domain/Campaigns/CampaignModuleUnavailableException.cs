namespace DungeonApp.Domain.Campaigns;

public sealed class CampaignModuleUnavailableException(ModuleId moduleId)
    : InvalidOperationException($"Campaign module '{moduleId}' is not registered by this application.");
