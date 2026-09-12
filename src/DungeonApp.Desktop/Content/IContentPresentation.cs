using Avalonia.Controls;
using DungeonApp.Core.Content;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// The shell's only window onto what an entry looks like. One method: hand over an entry, get back
/// a finished card.
/// <para>
/// The shell that calls <see cref="CreateCard"/> never sees the entry's content type - not as an
/// object, not as a discriminant, not as anything to switch on. There is no type-to-view map in the
/// shell, no <c>object</c> travelling through its view models waiting to be cast, and no casting
/// anywhere in this path. This is the structural half of the "Kontrakty są interfejsami" ban on a
/// tool introspecting a content type: the shell is not merely asked not to introspect, it has
/// physically nothing to introspect - <see cref="CreateCard"/>'s return value is already a finished
/// <see cref="Control"/>.
/// </para>
/// </summary>
public interface IContentPresentation
{
    /// <summary>
    /// Builds the finished card for <paramref name="entry"/>. Callers must never call this for an
    /// unresolved entry - one whose <see cref="RegisteredEntry.Type"/> is null - because there is no
    /// content set to build a card with; an implementation is free to throw rather than guess, since
    /// a caller that already has a <see cref="RegisteredEntry"/> can and must check
    /// <see cref="RegisteredEntry.Unresolved"/> first.
    /// </summary>
    Control CreateCard(Entry entry);
}
