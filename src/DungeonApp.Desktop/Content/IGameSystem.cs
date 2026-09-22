using System.Collections.Generic;
using DungeonApp.Core.Content;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// One compiled system, as the composition root sees it: it knows its own content types
/// (<see cref="IContentTypeCatalog"/>), how to draw them (<see cref="IContentPresentation"/>), a
/// human name for the selection screen (<see cref="DisplayName"/>), and the tabs it puts on the
/// sidebar once chosen - <see cref="SystemTabs"/> in the System category, <see cref="CampaignTabs"/>
/// in the Campaign category (docs/architecture.md, "Nawigacja: ekran wyboru systemu i pasek
/// boczny").
/// <para>
/// Today exactly one implementation exists (<c>Dnd5eSystem</c>), but nothing here or in the
/// composition root singles it out - <c>App.axaml.cs</c> holds an <see cref="IReadOnlyList{T}"/>
/// of these, never a single named instance, per docs/architecture.md's "Narzędzia biurka i system
/// okien".
/// </para>
/// </summary>
public interface IGameSystem : IContentTypeCatalog, IContentPresentation
{
    ContentId Id { get; }

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
}
