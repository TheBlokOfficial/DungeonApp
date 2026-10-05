using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonApp.Core.Entries;

/// <summary>
/// A system's declared presentation for one of its own content types, typed by the system's own
/// record (<typeparamref name="TRecord"/>) so every accessor below is compiled code reading real
/// properties - never a path into the data. This is the only place an <see cref="Entry"/>'s values
/// are opened while building a content tab; everything downstream only ever sees the erased
/// <see cref="IContentTypeProfile"/> this projects down to.
/// </summary>
public sealed class ContentTypeProfile<TRecord> : IContentTypeProfile
{
    private readonly Func<TRecord, string?>? _category;
    private readonly Func<TRecord, IReadOnlyList<string>> _tags;
    private readonly Func<TRecord, ContentBadge>? _badge;

    public ContentTypeProfile(
        ContentTypeReference type,
        ContentCategorySpec<TRecord>? category = null,
        Func<TRecord, IReadOnlyList<string>>? tags = null,
        Func<TRecord, ContentBadge>? badge = null,
        IReadOnlyList<ContentValueFilterSpec<TRecord>>? valueFilters = null,
        IReadOnlyList<ContentSortSpec<TRecord>>? sorts = null,
        bool showsPictureInRow = false)
    {
        Type = type;
        ShowsPictureInRow = showsPictureInRow;
        _category = category?.Value;
        CategoryLabel = category?.Label;
        _tags = tags ?? (_ => []);
        _badge = badge;

        ValueFilters = (valueFilters ?? [])
            .Select(spec => new ContentValueFilterDefinition(
                spec.Label,
                entry => spec.Value(entry.Values.Read<TRecord>()),
                spec.OptionOrder))
            .ToArray();

        Sorts = (sorts ?? [])
            .Select(spec => new ContentSortDefinition(
                spec.Label,
                Erase(spec.Compare),
                spec.IsTextual,
                spec.HasKey is { } hasKey ? Erase(hasKey) : null))
            .ToArray();
    }

    public ContentTypeReference Type { get; }

    public string? CategoryLabel { get; }

    public string? Category(Entry entry) => _category?.Invoke(entry.Values.Read<TRecord>());

    public IReadOnlyList<string> Tags(Entry entry) => _tags(entry.Values.Read<TRecord>());

    public ContentBadge Badge(Entry entry) => _badge is null ? default : _badge(entry.Values.Read<TRecord>());

    public bool ShowsPictureInRow { get; }

    public IReadOnlyList<ContentValueFilterDefinition> ValueFilters { get; }

    public IReadOnlyList<ContentSortDefinition> Sorts { get; }

    /// <summary>
    /// Widens a sort that only knows how to compare two <typeparamref name="TRecord"/>s into one
    /// that can sit beside every other content type's sorts in a tab naming more than one
    /// (<see cref="ContentTabDefinition.ContentTypes"/> is a list on purpose): an entry of a
    /// different content type compares equal under this sort - never first, never last on its own
    /// account - leaving the caller's own name tie-break to place it.
    /// </summary>
    private Comparison<Entry> Erase(Comparison<TRecord> compare) => (a, b) =>
    {
        var aMine = a.Type == Type;
        var bMine = b.Type == Type;

        return aMine && bMine ? compare(a.Values.Read<TRecord>(), b.Values.Read<TRecord>()) : 0;
    };

    /// <summary>
    /// Widens a sort's "has the key" test the same way: an entry of a different content type counts
    /// as having it, so it is never sent to the end on this sort's account.
    /// </summary>
    private Func<Entry, bool> Erase(Func<TRecord, bool> hasKey) => entry =>
        entry.Type != Type || hasKey(entry.Values.Read<TRecord>());
}
