using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Library.Entries.Desktop.Features.ContentTab;

/// <summary>
/// One rozwijany filter chip - "Kategoria", "Źródło", or one of a tab's own value filters
/// (docs/architecture.md, "Zakładki treści") - as a dropdown menu of options plus the library's own
/// always-present "Wszystkie" entry, which clears the filter back to null.
/// <para>
/// Lives for the whole life of its owning <see cref="ContentTabViewModel"/>, never rebuilt: a
/// rebuild only calls <see cref="SyncFromResult"/>, so the chip a view is bound to stays the same
/// instance across every search keystroke and filter change - nothing here forces a view to rebind.
/// </para>
/// </summary>
public sealed class ContentFilterChipViewModel : ObservableObject
{
    /// <summary>The dropdown's own "clear this filter" entry - never a real option value.</summary>
    public const string AllOptionsText = "Wszystkie";

    private readonly Action<string?> _apply;
    private string? _selected;
    private IReadOnlyList<string> _options;

    internal ContentFilterChipViewModel(string label, IReadOnlyList<string> options, Action<string?> apply)
    {
        Label = label;
        _options = options;
        _apply = apply;
    }

    public string Label { get; }

    public IReadOnlyList<string> Options => _options;

    /// <summary>The list a dropdown actually shows: "Wszystkie" first, then every real option.</summary>
    public IReadOnlyList<string> FlyoutItems => [AllOptionsText, .. _options];

    /// <summary>An empty option list means this filter has nothing to filter by - the chip is not drawn.</summary>
    public bool IsAvailable => _options.Count > 0;

    /// <summary>Whether a real (non-"Wszystkie") option is active - drives the chip's "on" look.</summary>
    public bool IsActive => _selected is not null;

    /// <summary>What the chip itself prints: the selected option, or the filter's own label when none is.</summary>
    public string ChipText => _selected ?? Label;

    /// <summary>Two-way bindable dropdown selection - "Wszystkie" round-trips to a null filter.</summary>
    public string SelectedDisplay
    {
        get => _selected ?? AllOptionsText;
        set
        {
            var next = value == AllOptionsText ? null : value;

            if (_selected == next)
            {
                return;
            }

            _selected = next;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsActive));
            RaisePropertyChanged(nameof(ChipText));
            _apply(next);
        }
    }

    /// <summary>
    /// Refreshes this chip's option list and current selection from a fresh
    /// <see cref="ContentListResult"/> - called after every rebuild, never in response to the chip's
    /// own <see cref="SelectedDisplay"/> setter (which already applied the change that caused the
    /// rebuild), so this never re-enters <see cref="_apply"/>.
    /// </summary>
    internal void SyncFromResult(IReadOnlyList<string> options, string? selected)
    {
        if (!_options.SequenceEqual(options, StringComparer.Ordinal))
        {
            _options = options;
            RaisePropertyChanged(nameof(Options));
            RaisePropertyChanged(nameof(FlyoutItems));
            RaisePropertyChanged(nameof(IsAvailable));
        }

        if (_selected != selected)
        {
            _selected = selected;
            RaisePropertyChanged(nameof(SelectedDisplay));
            RaisePropertyChanged(nameof(IsActive));
            RaisePropertyChanged(nameof(ChipText));
        }
    }
}
