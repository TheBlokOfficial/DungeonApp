using Avalonia.Controls;
using Avalonia.Media;
using DungeonApp.Library.Entries;

namespace DungeonApp.Library.Entries.Desktop.Content;

/// <summary>
/// The library's only window onto what an entry looks like. Implemented by a system (the only place
/// allowed to be concrete about a content type - docs/architecture.md, "Kontrakty są interfejsami"),
/// consumed by the library wherever it needs to draw a card, or color a badge, without knowing what
/// is inside one - the content tab skeleton (krok 10, zlecenie 2) today.
/// <para>
/// This used to be a frame contract (<c>DungeonApp.Desktop.Content.IContentPresentation</c>). It
/// moved here once the frame stopped needing to draw a card at all: every tab built from this
/// library's own skeleton is handed to the frame as a plain, already-finished tab - the frame never
/// calls either method below.
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

    /// <summary>
    /// Turns a row badge's <see cref="ContentBadge.ColorKey"/> into an actual brush - the one place
    /// allowed to, per docs/architecture.md's "Niezmiennik interfejsu": colors belonging to a system
    /// (rarity, today) live in the system, never borrowed from the frame's own meaning-carrying
    /// tokens. Called only when <see cref="ContentBadge.ColorKey"/> is not null; returns null for a
    /// key this system does not recognise, which the caller then draws the same plain, muted way as a
    /// badge with no color key at all - "klucz bez odpowiedzi" never throws.
    /// </summary>
    IBrush? ResolveBadgeBrush(string colorKey);
}
