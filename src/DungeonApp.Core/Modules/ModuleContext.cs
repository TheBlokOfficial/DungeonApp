using DungeonApp.Core.Events;

namespace DungeonApp.Core.Modules;

/// <summary>
/// What a module is handed when the campaign activates it. One per module, kept separate from a
/// shared instance so a future per-module concern has somewhere to attach without widening what
/// every module receives.
/// </summary>
public sealed class ModuleContext(CampaignModules modules, CampaignEvents events)
{
    /// <summary>
    /// The other modules in this campaign, for questions and instructions. Only ones declared in the
    /// manifest may be assumed.
    /// </summary>
    public CampaignModules Modules { get; } = modules;

    /// <summary>
    /// For announcing facts and hearing them. Never for asking another module to do something -
    /// that is what <see cref="Modules"/> is for.
    /// </summary>
    public CampaignEvents Events { get; } = events;
}
