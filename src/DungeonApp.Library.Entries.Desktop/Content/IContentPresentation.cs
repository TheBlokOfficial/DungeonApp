using Avalonia.Controls;
using DungeonApp.Core.Content;

namespace DungeonApp.Library.Entries.Desktop.Content;

/// <summary>
/// The library's only window onto what an entry looks like. One method: hand over an entry, get back
/// a finished card. Implemented by a system (the only place allowed to be concrete about a content
/// type - docs/architecture.md, "Kontrakty są interfejsami"), consumed by the library wherever it
/// needs to draw a card without knowing what is inside one - <c>RegistryViewModel</c> today.
/// <para>
/// This used to be a frame contract (<c>DungeonApp.Desktop.Content.IContentPresentation</c>). It
/// moved here once the frame stopped needing to draw a card at all: the registry tab is now built by
/// the system itself, from this library's own registry view, and handed to the frame as a plain,
/// already-finished tab - the frame never calls <see cref="CreateCard"/>.
/// </para>
/// </summary>
public interface IContentPresentation
{
    /// <summary>
    /// Builds the finished card for <paramref name="entry"/>. Callers must never call this for an
    /// unresolved entry - one whose <see cref="RegisteredEntry.Type"/> is null - because there is no
    /// system to build a card with; an implementation is free to throw rather than guess, since
    /// a caller that already has a <see cref="RegisteredEntry"/> can and must check
    /// <see cref="RegisteredEntry.Unresolved"/> first.
    /// </summary>
    Control CreateCard(Entry entry);
}
