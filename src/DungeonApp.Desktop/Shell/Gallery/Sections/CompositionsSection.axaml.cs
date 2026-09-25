using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

/// <summary>
/// One sample row of the Compositions section's list: a name, a type and a challenge rating.
/// Gallery data only - held in memory, never written anywhere.
/// </summary>
public sealed class SampleRow(string name, string type, string challenge)
{
    public string Name { get; } = name;

    public string Type { get; } = type;

    public string Challenge { get; } = challenge;
}

/// <summary>
/// The Compositions gallery section: frame blocks side by side. The list filters live by name
/// (case-insensitive) and by type chips (several chips = the union of their types). Filtering keeps
/// the row objects - rows are removed from and inserted into one visible collection, never replaced -
/// so a selected row that passes the filter stays selected. Deleting a row asks for confirmation and
/// offers "Cofnij", which puts the row back at its old place. Everything stays in this view's memory.
/// </summary>
public partial class CompositionsSection : UserControl
{
    private readonly List<SampleRow> _all =
    [
        new("Bandyta", "Humanoid", "1/8"),
        new("Goblin", "Humanoid", "1/4"),
        new("Hobgoblin", "Humanoid", "1/2"),
        new("Ork", "Humanoid", "1/2"),
        new("Wilk", "Bestia", "1/4"),
        new("Olbrzymi pająk", "Bestia", "1"),
        new("Niedźwiedź brunatny", "Bestia", "1"),
        new("Szkielet", "Nieumarły", "1/4"),
        new("Zombie", "Nieumarły", "1/4"),
        new("Upiór", "Nieumarły", "5"),
        new("Pisklę czerwonego smoka", "Smok", "4"),
        new("Młody zielony smok", "Smok", "8"),
    ];

    private readonly ObservableCollection<SampleRow> _visible = [];

    public CompositionsSection()
    {
        InitializeComponent();

        SampleList.ItemsSource = _visible;
        SearchBox.TextChanged += (_, _) => ApplyFilter();
        foreach (var chip in TypeChips.Children.OfType<ToggleButton>())
        {
            chip.IsCheckedChanged += (_, _) => ApplyFilter();
        }

        ClearFiltersButton.Click += (_, _) => ClearFilters();
        NoResultsClearButton.Click += (_, _) => ClearFilters();
        ApplyFilter();
    }

    private void ClearFilters()
    {
        SearchBox.Text = string.Empty;
        foreach (var chip in TypeChips.Children.OfType<ToggleButton>())
        {
            chip.IsChecked = false;
        }
    }

    private void ApplyFilter()
    {
        var text = SearchBox.Text?.Trim() ?? string.Empty;
        var types = TypeChips.Children.OfType<ToggleButton>()
            .Where(chip => chip.IsChecked == true)
            .Select(chip => chip.Content as string)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        var matches = _all
            .Where(row => text.Length == 0 || row.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase))
            .Where(row => types.Count == 0 || types.Contains(row.Type))
            .ToList();

        // Remove what no longer passes, then insert what is missing at its place in the full order.
        // Rows that pass stay the same objects, so the list keeps their selection.
        for (var i = _visible.Count - 1; i >= 0; i--)
        {
            if (!matches.Contains(_visible[i]))
            {
                _visible.RemoveAt(i);
            }
        }

        for (var i = 0; i < matches.Count; i++)
        {
            if (i >= _visible.Count || !ReferenceEquals(_visible[i], matches[i]))
            {
                _visible.Insert(i, matches[i]);
            }
        }

        var isFiltered = text.Length > 0 || types.Count > 0;
        ClearFiltersButton.IsEnabled = isFiltered;
        NoResultsClearButton.IsEnabled = isFiltered;
        NoResultsClearButton.IsVisible = isFiltered;
        CountText.Text = $"{matches.Count} {EntryPlural(matches.Count)}";

        var hasRows = matches.Count > 0;
        SampleList.IsVisible = hasRows;
        NoResults.IsVisible = !hasRows;
        NoResultsMessage.Message = isFiltered
            ? "Nic nie pasuje do filtrów i wyszukiwania."
            : "Lista jest pusta.";
    }

    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: SampleRow row } button)
        {
            return;
        }

        var confirmed = await ConfirmationDialog.ShowAsync(
            button,
            "Usunąć potwora?",
            $"„{row.Name}” zniknie z listy. Zaraz po usunięciu można to cofnąć.",
            "Usuń",
            isDestructive: true);
        if (!confirmed)
        {
            return;
        }

        var index = _all.IndexOf(row);
        if (index < 0)
        {
            return;
        }

        _all.RemoveAt(index);
        ApplyFilter();

        // The row's button has left the window with the row; the section is still in it.
        NotificationToast.Show(
            this,
            NotificationKind.Information,
            $"Usunięto „{row.Name}”.",
            "Cofnij",
            () => Restore(row, index));
    }

    private void Restore(SampleRow row, int index)
    {
        if (_all.Contains(row))
        {
            return;
        }

        _all.Insert(Math.Min(index, _all.Count), row);
        ApplyFilter();
    }

    /// <summary>
    /// Polish plural of "wpis": 1 wpis, 2-4 wpisy (also 22-24, 32-34...), otherwise wpisów
    /// (0, 5-21, 25...). The same rule as ContentTabViewModel's private helper in
    /// DungeonApp.Library.Entries.Desktop, which this project cannot reference.
    /// </summary>
    private static string EntryPlural(int count)
    {
        if (count == 1)
        {
            return "wpis";
        }

        var lastDigit = count % 10;
        var lastTwoDigits = count % 100;
        return lastDigit is >= 2 and <= 4 && lastTwoDigits is < 12 or > 14 ? "wpisy" : "wpisów";
    }
}
