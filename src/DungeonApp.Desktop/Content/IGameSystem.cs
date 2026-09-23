using System.Collections.Generic;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Startup;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// One compiled system, as the composition root sees it. Deliberately narrow
/// (docs/architecture.md, "Rama, biblioteka, system"): the frame is told only who a system is
/// (<see cref="Id"/>, <see cref="DisplayName"/>), what tabs it puts on the sidebar once chosen -
/// <see cref="SystemTabs"/> in the System category, <see cref="CampaignTabs"/> in the Campaign
/// category - what state models its campaigns keep (<see cref="StateModels"/>), and what startup
/// work it wants run (<see cref="StartupSteps"/>). It is no longer, for the frame, a catalog of
/// content types or a way to draw a card - that knowledge stays entirely on the concrete system
/// class, used only by the library and by the system's own tab factories.
/// <para>
/// Today exactly one implementation exists (<c>Dnd5eSystem</c>), but nothing here or in the
/// composition root singles it out - <c>App.axaml.cs</c> holds an <see cref="IReadOnlyList{T}"/>
/// of these, never a single named instance, per docs/architecture.md's "Narzędzia biurka i system
/// okien".
/// </para>
/// </summary>
public interface IGameSystem
{
    SystemId Id { get; }

    /// <summary>The name shown on the fullscreen system-selection screen.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Constant declarations, read once when this system is chosen. Each factory is synchronous and
    /// receives a <see cref="SystemTabContext"/> - narrower than <see cref="CampaignTabContext"/>,
    /// because a System-category tab never sees the open campaign at all.
    /// </summary>
    IReadOnlyList<SystemTabDeclaration> SystemTabs { get; }

    /// <summary>
    /// Constant declarations, read once when this system is chosen. Each factory is asynchronous and
    /// is invoked again - fresh - for every campaign the GM opens, with a
    /// <see cref="CampaignTabContext"/> built for that one campaign.
    /// </summary>
    IReadOnlyList<CampaignTabDeclaration> CampaignTabs { get; }

    /// <summary>
    /// The state models this system's campaigns keep - docs/architecture.md, "Gdzie mieszka stan":
    /// "Rama zapisuje, system deklaruje." Read once, when a campaign is opened or created, and
    /// handed to the repository so it knows which files to write and read; the frame never names
    /// any of these models itself.
    /// </summary>
    IReadOnlyList<StateModelDeclaration> StateModels { get; }

    /// <summary>
    /// Startup work this system wants run behind the loading curtain - loading its own content packs,
    /// warming its own cards, and anything else its own concern (docs/architecture.md, "Start
    /// aplikacji"). The frame runs every step here without knowing what any of them does; a step that
    /// fails degrades startup the same way one of the frame's own steps does (see
    /// <see cref="IStartupStep.FailureWarning"/>), never blocking entry to the program.
    /// </summary>
    IReadOnlyList<IStartupStep> StartupSteps { get; }
}
