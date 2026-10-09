using System.Collections.Generic;
using DungeonApp.Core.Entries;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.Workspace.Panels;

namespace DungeonApp.Desktop.Systems;

/// <summary>
/// One compiled system, as the composition root sees it. Deliberately narrow: the frame is told
/// only who a system is (<see cref="Id"/>, <see cref="DisplayName"/>), what tabs it puts on the
/// sidebar once chosen - <see cref="SystemTabs"/> in the System category,
/// <see cref="CampaignTabs"/> in the Campaign category under the desk - what tools it puts on the
/// desk (<see cref="CreateDeskTools"/>), what state models its campaigns keep
/// (<see cref="StateModels"/>), and what startup work it wants run (<see cref="StartupSteps"/>). It
/// is not, for the frame, a catalog of content types or a way to draw a card - that knowledge stays
/// entirely on the concrete system class, used only by the library and by the system's own tab
/// factories.
/// <para>
/// Today exactly one implementation of a game system exists, but nothing here or in the
/// composition root singles it out - <c>App.axaml.cs</c> holds an <see cref="IReadOnlyList{T}"/>
/// of these, never a single named instance.
/// </para>
/// </summary>
public interface IGameSystem
{
    SystemId Id { get; }

    /// <summary>The name shown on the fullscreen system-selection screen.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Constant declarations, read once when this system is chosen. Each factory is synchronous and
    /// takes no argument - unlike a campaign tab's <see cref="CampaignTabContext"/> - because a
    /// System-category tab never sees the open campaign at all.
    /// </summary>
    IReadOnlyList<SystemTabDeclaration> SystemTabs { get; }

    /// <summary>
    /// Constant declarations, read once when this system is chosen. Each factory is asynchronous and
    /// is invoked again - fresh - for every campaign the GM opens, with a
    /// <see cref="CampaignTabContext"/> built for that one campaign.
    /// </summary>
    IReadOnlyList<CampaignTabDeclaration> CampaignTabs { get; }

    /// <summary>
    /// The tools this system puts on the desk of the open campaign. The desk itself is the frame's
    /// campaign position; the system only stocks it. Called once for every campaign the GM opens,
    /// with a <see cref="CampaignTabContext"/> built for that one campaign.
    /// </summary>
    IReadOnlyList<WorkspacePanelDescriptor> CreateDeskTools(CampaignTabContext context);

    /// <summary>
    /// The state models this system's campaigns keep on top of the frame's own (entities, world
    /// folders and the № counter - <see cref="DungeonApp.Core.World.WorldModels"/>). Read once, when a
    /// campaign is opened or created, and combined with the frame's models into the list handed to
    /// the repository; the frame never names any of these models itself.
    /// </summary>
    IReadOnlyList<StateModelDeclaration> StateModels { get; }

    /// <summary>
    /// The content types whose entries can become entities in the world tree, each with its icon.
    /// A type that is not listed (knowledge such as a condition) has no entities; the tree asks
    /// nothing else about what a type is.
    /// </summary>
    IReadOnlyList<WorldEntityType> EntityTypes { get; }

    /// <summary>
    /// The short text at the right edge of an entity's tree row - for a being with combat values
    /// the current and maximum health ("7/7"); <see langword="null"/> when this entity has none.
    /// Only a resolved entity has values to read.
    /// </summary>
    string? RowHint(ResolvedEntity entity);

    /// <summary>
    /// Startup work this system wants run behind the loading curtain - loading its own content packs,
    /// warming its own cards, and anything else its own concern. The frame runs every step here
    /// without knowing what any of them does; a step that fails degrades startup the same way one
    /// of the frame's own steps does (see <see cref="IStartupStep.FailureWarning"/>), never
    /// blocking entry to the program.
    /// </summary>
    IReadOnlyList<IStartupStep> StartupSteps { get; }
}
