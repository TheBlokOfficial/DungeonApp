namespace DungeonApp.Domain.Campaigns;

public sealed class CampaignModuleAlreadyEnabledException(ModuleId moduleId)
    : InvalidOperationException($"Campaign module '{moduleId}' is already enabled.");
