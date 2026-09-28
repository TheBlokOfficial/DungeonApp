using System;
using System.Collections.Generic;

namespace DungeonApp.Library.Entries;

/// <summary>
/// The library's only window onto one content type's presentation inside a content tab
/// (docs/architecture.md, "Zakładki treści"): which value is its category, its tags, its row
/// badge, and what it offers to filter and sort by. Every accessor takes the raw <see cref="Entry"/>
/// the registry already holds; only a <see cref="ContentTypeProfile{TRecord}"/> - the one
/// implementation this interface ever has - opens it, and this interface is the only shape
/// <c>ContentListModel</c> is allowed to know. Mirrors <c>IContentPresentation</c>'s own
/// discipline: a system supplies compiled code, the library never introspects a content type.
/// </summary>
public interface IContentTypeProfile
{
    /// <summary>The content type this profile speaks for - what routes an entry to it.</summary>
    ContentTypeReference Type { get; }

    /// <summary>
    /// The name of this content type's category filter ("Grupa"), or null when the content type has
    /// no category - a tab none of whose types names one has no category filter at all.
    /// </summary>
    string? CategoryLabel { get; }

    /// <summary>The entry's category, or null when it has none (or its content type has no category).</summary>
    string? Category(Entry entry);

    /// <summary>The entry's tags, shown on its detail header. Never null - an empty list for "none".</summary>
    IReadOnlyList<string> Tags(Entry entry);

    /// <summary>The entry's row badge.</summary>
    ContentBadge Badge(Entry entry);

    /// <summary>Zero or more filterable value dimensions this content type offers.</summary>
    IReadOnlyList<ContentValueFilterDefinition> ValueFilters { get; }

    /// <summary>Zero or more sorts this content type offers, beyond the library's own default.</summary>
    IReadOnlyList<ContentSortDefinition> Sorts { get; }
}

/// <summary>
/// The erased form of a <see cref="ContentValueFilterSpec{TRecord}"/>: the same label and option
/// order, but reading through the already-opened <see cref="Entry"/> rather than a concrete record.
/// </summary>
public sealed record ContentValueFilterDefinition(
    string Label,
    Func<Entry, string?> Value,
    IComparer<string> OptionOrder);

/// <summary>
/// The erased form of a <see cref="ContentSortSpec{TRecord}"/>: comparing two <see cref="Entry"/>
/// values rather than two concrete records. <see cref="ContentTypeProfile{TRecord}"/> arranges for
/// this to compare as equal whenever either side is not its own content type, so it can sit safely
/// among a mixed tab's other sorts - see its own remarks.
/// </summary>
public sealed record ContentSortDefinition(string Label, Comparison<Entry> Compare, bool IsTextual = false);
