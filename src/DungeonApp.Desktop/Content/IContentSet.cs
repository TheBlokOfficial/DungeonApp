using DungeonApp.Core.Content;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// One compiled content set, as the composition root sees it: it knows its own content types
/// (<see cref="IContentTypeCatalog"/>) and how to draw them (<see cref="IContentPresentation"/>),
/// and it names itself (<see cref="Id"/>) so an aggregate over several content sets can find the
/// right one for a given reference.
/// <para>
/// Today exactly one implementation exists (<c>Dnd5eContentSet</c>), but nothing here or in the
/// composition root singles it out - <c>App.axaml.cs</c> holds an <see cref="System.Collections.Generic.IReadOnlyList{T}"/>
/// of these, never a single named instance, per docs/architecture.md's "Narzędzia biurka i system
/// okien".
/// </para>
/// </summary>
public interface IContentSet : IContentTypeCatalog, IContentPresentation
{
    ContentId Id { get; }
}
