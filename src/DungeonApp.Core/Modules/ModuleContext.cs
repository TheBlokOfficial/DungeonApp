namespace DungeonApp.Core.Modules;

/// <summary>
/// What a module is handed when the campaign activates it.
/// <para>
/// A type rather than a bare registry argument so that the things a module will need next - the
/// journal, and later the announcement channel - can arrive without changing every module's
/// signature. Today it carries one thing, and that is honest: nothing else exists yet.
/// </para>
/// </summary>
public sealed class ModuleContext(CampaignModules modules)
{
    /// <summary>The other modules in this campaign. Only ones declared in the manifest may be assumed.</summary>
    public CampaignModules Modules { get; } = modules;
}
