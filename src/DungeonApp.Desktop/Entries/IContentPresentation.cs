using Avalonia.Controls;
using Avalonia.Media;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries;

/// <summary>
/// The library's only window onto what an entry looks like. Implemented by a system (the only place
/// allowed to be concrete about a content type), consumed by the library wherever it needs to draw
/// a card, or color a badge, without knowing what is inside one - the content tab skeleton today.
/// <para>
/// Not a frame contract: every tab built from this library's own skeleton is handed to the frame as
/// a plain, already-finished tab - the frame never calls either method below.
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
    /// <para>
    /// <paramref name="picture"/> is the entry's picture already read from its pack
    /// (<see cref="EntryPicture.Load(ContentRegistry, RegisteredEntry)"/>), or
    /// <see cref="EntryPicture.None"/>: the card decides where its frame stands and which placeholder
    /// icon it shows, but never where the entry's pack lies - an <see cref="Entry"/> does not know.
    /// </para>
    /// </summary>
    Control CreateCard(Entry entry, EntryPicture picture);

    /// <summary>
    /// Turns a row badge's <see cref="ContentBadge.ColorKey"/> into an actual brush - the one place
    /// allowed to: colors belonging to a system (rarity, today) live in the system, never borrowed
    /// from the frame's own meaning-carrying tokens. Called only when
    /// <see cref="ContentBadge.ColorKey"/> is not null; returns null for a key this system does not
    /// recognise, which the caller then draws the same plain, muted way as a badge with no color
    /// key at all - an unrecognised key never throws.
    /// </summary>
    IBrush? ResolveBadgeBrush(string colorKey);
}
