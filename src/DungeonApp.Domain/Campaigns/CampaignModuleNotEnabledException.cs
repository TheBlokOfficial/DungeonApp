namespace DungeonApp.Domain.Campaigns;

public sealed class CampaignModuleNotEnabledException(ModuleId moduleId)
    : InvalidOperationException($"Campaign module '{moduleId}' is not enabled.");
