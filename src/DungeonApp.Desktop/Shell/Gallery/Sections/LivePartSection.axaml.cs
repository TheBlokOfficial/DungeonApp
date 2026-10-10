using System;
using System.Globalization;
using Avalonia.Controls;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

/// <summary>Sample values for the live part's controls.</summary>
public partial class LivePartSection : UserControl
{
    // The working tracked value: its arithmetic stands in for a system's (subtract down to 0, add up
    // to the maximum, set anything); the reference point is the value it starts with, so the blot
    // shows whenever the value differs from it.
    private const int StartingValue = 9;
    private const int SampleMaximum = 13;
    private int _current = StartingValue;

    public LivePartSection()
    {
        InitializeComponent();
        FillTrackedValueSample();
        FillNoteSamples();
        FillBaseSamples();
    }

    private void FillTrackedValueSample()
    {
        LiveValue.Preview = Preview;
        LiveValue.ChangeAccepted += (_, e) =>
        {
            _current = Apply(_current, e.Change).Result;
            LiveValue.Value = _current.ToString(CultureInfo.InvariantCulture);
            LiveValue.IsUncommitted = _current != StartingValue;
        };
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

    private string Preview(NumberChange change)
    {
        var (result, beyond) = Apply(_current, change);
        var shown = $"{_current} → {result}";
        return change.Kind switch
        {
            NumberChangeKind.Subtract when beyond > 0 => $"{shown} · {beyond} poniżej zera",
            NumberChangeKind.Add when beyond > 0 => $"{shown} · {beyond} ponad maksimum",
            _ => shown,
        };
    }

    // Beyond: how much of a subtraction fell below zero, or of an addition rose above the maximum.
    private static (int Result, int Beyond) Apply(int current, NumberChange change) => change.Kind switch
    {
        NumberChangeKind.Subtract => (Math.Max(0, current - change.Amount), Math.Max(0, change.Amount - current)),
        NumberChangeKind.Add => (Math.Min(Math.Max(current, SampleMaximum), current + change.Amount),
            Math.Max(0, current + change.Amount - Math.Max(current, SampleMaximum))),
        _ => (change.Amount, 0),
    };
}
