using System.Collections.Generic;
using Avalonia.Controls;

namespace DungeonApp.Library.Entries.Desktop.Features.ContentTab;

/// <summary>
/// What the detail column shows for whichever row is selected - null when nothing is. Mirrors
/// <see cref="ContentSelectionDetail"/>'s shapes, translated into what
/// <see cref="DungeonApp.Library.Entries.Desktop.Controls.EntryDetailView"/> draws: a resolved entry's header and finished card, or a
/// broken row's header, full reason and file path.
/// </summary>
public abstract class ContentDetailViewModel
{
    private protected ContentDetailViewModel(string overline, string name)
    {
        Overline = overline;
        Name = name;
    }

    /// <summary>The small line over the name, as written - the view draws it in capitals.</summary>
    public string Overline { get; }

    public string Name { get; }
}

/// <summary>
/// A resolved entry's detail: "{kategoria} · {paczka}" (just the pack when the entry has no
/// category) over the name, its tags, and the system's own finished card underneath - all drawn by
/// the library's detail block except the card itself.
/// </summary>
public sealed class ValidContentDetailViewModel(
    string overline, string name, IReadOnlyList<string> tags, Control card)
    : ContentDetailViewModel(overline, name)
{
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
    : ContentDetailViewModel(overline, name)
{
    public string Reason { get; } = reason;

    public string? Path { get; } = path;

    public bool HasPath => Path is not null;
}
