using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

/// <summary>Sample values for the live part's controls.</summary>
public partial class LivePartSection : UserControl
{
    public LivePartSection()
    {
        InitializeComponent();
        FillTrackedValueSamples();
        FillNoteSamples();
        FillBaseSamples();
    }

    // Every tracked value in the gallery takes a change, each with its own arithmetic standing in for
    // a system's (subtract down to 0, add up to the maximum, set anything). The reference point is the
    // value it starts with, so the blot shows whenever the value differs from it; a sample that starts
    // with the blot keeps it.
    private void FillTrackedValueSamples()
    {
        foreach (var tile in this.GetLogicalDescendants().OfType<TrackedValueTile>())
        {
            var start = ParseOrZero(tile.Value);
            var maximum = ParseOrZero(tile.Maximum);
            var alwaysUncommitted = tile.IsUncommitted;
            var current = start;

            tile.Preview = change => Preview(current, maximum, change);
            tile.ChangeAccepted += (_, e) =>
            {
                current = Apply(current, maximum, e.Change).Result;
                tile.Value = current.ToString(CultureInfo.InvariantCulture);
                tile.IsUncommitted = alwaysUncommitted || current != start;
            };
        }
    }

    private void FillNoteSamples()
    {
        NoteShort.Text = "Boi się psów.";
        NoteOneLine.Text = "Przekupiony przez gildię złodziei.";
        NoteLines.Text = "Ukradł klucz do skarbca — trzyma go w lewej sakwie.\nUcieknie, gdy zostanie sam.";
        NoteDisabled.Text = "Nie wie, że drużyna go śledzi.";

        // The working sample shows what its keeper would receive on leaving the field.
        NoteLines.Committed += (_, e) => NoteCommitted.Text = $"Zatwierdzono notatkę, znaków: {e.Text.Length}.";
    }

    // Written out, not computed: the frame has no arithmetic of any system. The lowered score keeps
    // a positive modifier, so the cell tint and the digit colour are seen to mean different things.
    private void FillBaseSamples()
    {
        BaseLeftTable.Rows =
        [
            new AbilityRow("SIŁ", "8", "−1", ValueTone.Negative),
            new AbilityRow("ZRĘ", "12", "+1", ValueTone.Positive, BaseDeviation.Below),
            new AbilityRow("KON", "10", "+0"),
        ];
        BaseRightTable.Rows =
        [
            new AbilityRow("INT", "10", "+0"),
            new AbilityRow("MDR", "8", "−1", ValueTone.Negative),
            new AbilityRow("CHA", "18", "+4", ValueTone.Positive, BaseDeviation.Above),
        ];
    }

    private static int ParseOrZero(string? text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : 0;

    private static string Preview(int current, int maximum, NumberChange change)
    {
        var (result, belowZero) = Apply(current, maximum, change);
        var shown = $"{current} → {result}";
        return belowZero > 0 ? $"{shown} · {belowZero}" : shown;
    }

    // BelowZero: how much of a subtraction fell below zero.
    private static (int Result, int BelowZero) Apply(int current, int maximum, NumberChange change) => change.Kind switch
    {
        NumberChangeKind.Subtract => (Math.Max(0, current - change.Amount), Math.Max(0, change.Amount - current)),
        NumberChangeKind.Add => (Math.Min(Math.Max(current, maximum), current + change.Amount), 0),
        _ => (change.Amount, 0),
    };
}
