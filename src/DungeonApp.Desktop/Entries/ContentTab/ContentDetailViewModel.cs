using System.Collections.Generic;
using Avalonia.Controls;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries.ContentTab;

/// <summary>
/// What the detail column shows for whichever row is selected - null when nothing is. Mirrors
/// <see cref="ContentSelectionDetail"/>'s shapes, translated into what
/// <see cref="DungeonApp.Desktop.Entries.Controls.EntryDetailView"/> draws: a resolved entry's header and finished card, or a
/// broken row's header, full reason and file path.
/// </summary>
public abstract class ContentDetailViewModel
{
    private protected ContentDetailViewModel(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

/// <summary>
/// A resolved entry's detail: the "{tytuł zakładki} › {kategoria} › {nazwa}" path (the category
/// segment left out when the entry has none) over the name, its tags, and the system's own finished
/// card underneath - all drawn by the library's detail block except the card itself.
/// </summary>
public sealed class ValidContentDetailViewModel(
    IReadOnlyList<string> breadcrumbs, string name, IReadOnlyList<string> tags, Control card)
    : ContentDetailViewModel(name)
{
    /// <summary>The path's segments, the last one being the entry itself.</summary>
    public IReadOnlyList<string> Breadcrumbs { get; } = breadcrumbs;

    public IReadOnlyList<string> Tags { get; } = tags;

    public bool HasTags { get; } = tags.Count > 0;

    public Control Card { get; } = card;
}

/// <summary>
/// A broken row's or rejected pack's detail: "Wpis niewczytany · {paczka}" or "Paczka odrzucona",
/// its own name (or file/pack location, whichever the list shows), the full, selectable reason -
/// never truncated, never reworded - and the file or directory path when there is one.
/// </summary>
public sealed class BrokenContentDetailViewModel(string overline, string name, string reason, string? path)
    : ContentDetailViewModel(name)
{
    /// <summary>The small line over the name, as written - the view draws it in capitals.</summary>
    public string Overline { get; } = overline;

    public string Reason { get; } = reason;

    public string? Path { get; } = path;

    public bool HasPath => Path is not null;
}
