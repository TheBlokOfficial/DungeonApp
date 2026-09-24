using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

namespace DungeonApp.Library.Entries.Desktop.Features.ContentTab;

/// <summary>
/// What the detail column shows for whichever row is selected - null when nothing is (the shell
/// falls back to the same selection prompt the old registry showed). Mirrors
/// <see cref="ContentSelectionDetail"/>'s own two shapes, translated into what a view actually draws:
/// a resolved entry's breadcrumbs, category, name, tags and finished card, or a broken row's name and
/// full reason.
/// </summary>
public abstract class ContentDetailViewModel;

/// <summary>
/// A resolved entry's detail: "<tytuł zakładki> › <kategoria> › <nazwa>" breadcrumbs (the category
/// segment left out when the entry has none), the category label over the name, the name, its tags,
/// and the system's own finished card underneath - all drawn by the skeleton except the card itself.
/// </summary>
public sealed class ValidContentDetailViewModel(
    IReadOnlyList<string> breadcrumbs, string? category, string name, IReadOnlyList<string> tags, Control card)
    : ContentDetailViewModel
{
    public IReadOnlyList<string> Breadcrumbs { get; } = breadcrumbs;

    /// <summary>
    /// Every breadcrumb but the last, joined with "›" (krok 10, brief A8) - the leading, muted part
    /// of the trail. Empty when <see cref="Breadcrumbs"/> has only one segment.
    /// </summary>
    public string BreadcrumbTrailText { get; } = string.Join(" › ", breadcrumbs.Take(breadcrumbs.Count - 1));

    public bool HasBreadcrumbTrail => BreadcrumbTrailText.Length > 0;

    /// <summary>The breadcrumb trail's last segment - the current entry's own name, shown less muted than the rest of the trail.</summary>
    public string BreadcrumbCurrent { get; } = breadcrumbs[^1];

    public string? Category { get; } = category;

    public bool HasCategory => Category is not null;

    public string Name { get; } = name;

    public IReadOnlyList<string> Tags { get; } = tags;

    public bool HasTags { get; } = tags.Count > 0;

    public Control Card { get; } = card;
}

/// <summary>
/// A broken row's or rejected pack's detail: its own name (or file/pack location, whichever
/// <see cref="ContentBrokenRow.DisplayName"/> or <see cref="RejectedPack.Location"/> already is) and
/// the full, selectable reason - never truncated, never reworded.
/// </summary>
public sealed class BrokenContentDetailViewModel(string name, string reason) : ContentDetailViewModel
{
    public string Name { get; } = name;

    public string Reason { get; } = reason;
}
