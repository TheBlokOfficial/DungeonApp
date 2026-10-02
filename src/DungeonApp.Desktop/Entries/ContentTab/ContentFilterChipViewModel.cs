using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using DungeonApp.Desktop.ViewModels;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries.ContentTab;

/// <summary>
/// One filter chip - the category, one of a tab's own value filters, or "Paczka" - as a list of
/// options with check boxes: any number of them chosen, nothing chosen means the filter does not
/// narrow. There is no "Wszystkie" entry.
/// <para>
/// The chosen values are kept by value (the option text), never by object, in
/// <see cref="SelectedValues"/> - the collection the chip's picker adds to and removes from. Lives
/// for the whole life of its owning <see cref="ContentTabViewModel"/>, never rebuilt: a rebuild only
/// calls <see cref="SyncFromResult"/>, so the chip a view is bound to stays the same instance across
/// every search keystroke and filter change.
/// </para>
/// </summary>
public sealed class ContentFilterChipViewModel : ObservableObject
{
    private readonly Action<IReadOnlyCollection<string>> _apply;
    private IReadOnlyList<string> _options;
    private bool _syncing;

    internal ContentFilterChipViewModel(string label, IReadOnlyList<string> options, Action<IReadOnlyCollection<string>> apply)
    {
        Label = label;
        _options = options;
        _apply = apply;
        SelectedValues.CollectionChanged += OnSelectedValuesChanged;
    }

    /// <summary>The filter's name - what the chip says while nothing is chosen.</summary>
    public string Label { get; }

    public IReadOnlyList<string> Options => _options;

    /// <summary>An empty option list means this filter has nothing to filter by - the chip is disabled.</summary>
    public bool IsAvailable => _options.Count > 0;

    /// <summary>The chosen option texts - the picker's own selection list, toggled by its check boxes.</summary>
    public ObservableCollection<string> SelectedValues { get; } = [];

    /// <summary>Whether anything is chosen - the filter narrows the list.</summary>
    public bool IsActive => SelectedValues.Count > 0;

    /// <summary>
    /// Refreshes this chip's option list and chosen values from the state a rebuild was made for -
    /// never re-enters <see cref="_apply"/>: the change that caused the rebuild is already applied.
    /// </summary>
    internal void SyncFromResult(IReadOnlyList<string> options, IReadOnlyCollection<string>? selected)
    {
        if (!_options.SequenceEqual(options, StringComparer.Ordinal))
        {
            _options = options;
            RaisePropertyChanged(nameof(Options));
            RaisePropertyChanged(nameof(IsAvailable));
        }

        var next = selected ?? [];
        if (SelectedValues.Count == next.Count && SelectedValues.All(next.Contains))
        {
            return;
        }

        _syncing = true;
        try
        {
            SelectedValues.Clear();
            foreach (var value in next)
            {
                SelectedValues.Add(value);
            }
        }
        finally
        {
            _syncing = false;
        }

        RaisePropertyChanged(nameof(IsActive));
    }

    // The picker toggled a value: the chip hands its whole choice to the list state. A sync from a
    // rebuild writes the same collection and is not a choice.
    private void OnSelectedValuesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        RaisePropertyChanged(nameof(IsActive));
        _apply(SelectedValues.ToArray());
    }
}
