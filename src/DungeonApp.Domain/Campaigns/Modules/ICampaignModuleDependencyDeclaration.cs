namespace DungeonApp.Domain.Campaigns.Modules;

/// <summary>
/// Optional declaration of modules that must be enabled before this module can be enabled.
/// </summary>
public interface ICampaignModuleDependencyDeclaration
{
    IReadOnlyCollection<ModuleId> RequiredModuleIds { get; }
}
