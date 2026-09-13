using System.Collections.Generic;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// One compiled content set, as the composition root sees it: it knows its own content types
/// (<see cref="IContentTypeCatalog"/>), how to draw them (<see cref="IContentPresentation"/>), and
/// which desk tools it brings ("tool belt" - <see cref="CreateTools"/>); it names itself
/// (<see cref="Id"/>) so an aggregate over several content sets can find the right one for a given
/// reference.
/// <para>
/// Today exactly one implementation exists (<c>Dnd5eContentSet</c>), but nothing here or in the
/// composition root singles it out - <c>App.axaml.cs</c> holds an <see cref="IReadOnlyList{T}"/>
/// of these, never a single named instance, per docs/architecture.md's "Narzędzia biurka i system
/// okien".
/// </para>
/// </summary>
public interface IContentSet : IContentTypeCatalog, IContentPresentation
{
    ContentId Id { get; }

    /// <summary>
    /// The desk tools this content set brings, built against the campaign named by
    /// <paramref name="context"/>. A content set is the only place allowed to know what its own content
    /// looks like, so it is also the only place allowed to bring a tool that reads one of its fields
    /// by name - the shell never lists a tool here itself, and never learns what any of them are for.
    /// Returns an empty list when this content set brings none.
    /// </summary>
    IReadOnlyList<WorkspacePanelDescriptor> CreateTools(CampaignToolContext context);
}
