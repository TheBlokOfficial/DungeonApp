using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonApp.Library.Entries;

/// <summary>
/// A system's declared presentation for one of its own content types, typed by the system's own
/// record (<typeparamref name="TRecord"/>) so every accessor below is compiled code reading real
/// properties - never a path into the data, per docs/architecture.md's "Zakładki treści". This is
/// the only place an <see cref="Entry"/>'s values are opened while building a content tab;
/// everything downstream only ever sees the erased <see cref="IContentTypeProfile"/> this projects
/// down to.
/// </summary>
public sealed class ContentTypeProfile<TRecord> : IContentTypeProfile
{
    private readonly Func<TRecord, string?>? _category;
    private readonly Func<TRecord, IReadOnlyList<string>> _tags;
    private readonly Func<TRecord, ContentBadge>? _badge;

    public ContentTypeProfile(
        ContentTypeReference type,
        Func<TRecord, string?>? category = null,
        Func<TRecord, IReadOnlyList<string>>? tags = null,
        Func<TRecord, ContentBadge>? badge = null,
        IReadOnlyList<ContentValueFilterSpec<TRecord>>? valueFilters = null,
        IReadOnlyList<ContentSortSpec<TRecord>>? sorts = null)
    {
        Type = type;
        _category = category;
        _tags = tags ?? (_ => []);
        _badge = badge;

        ValueFilters = (valueFilters ?? [])
            .Select(spec => new ContentValueFilterDefinition(
                spec.Label,
                entry => spec.Value(entry.Values.Read<TRecord>()),
                spec.OptionOrder))
            .ToArray();

        Sorts = (sorts ?? [])
            .Select(spec => new ContentSortDefinition(spec.Label, Erase(spec.Compare)))
            .ToArray();
    }

    public ContentTypeReference Type { get; }

    public string? Category(Entry entry) => _category?.Invoke(entry.Values.Read<TRecord>());

    public IReadOnlyList<string> Tags(Entry entry) => _tags(entry.Values.Read<TRecord>());

    public ContentBadge Badge(Entry entry) => _badge is null ? default : _badge(entry.Values.Read<TRecord>());

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
}
