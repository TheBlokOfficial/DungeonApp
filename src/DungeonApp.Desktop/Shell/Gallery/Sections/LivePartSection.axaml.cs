using System;
using System.Globalization;
using Avalonia.Controls;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Controls;

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

        LiveValue.Preview = Preview;
        LiveValue.ChangeAccepted += (_, e) =>
        {
            _current = Apply(_current, e.Change).Result;
            LiveValue.Value = _current.ToString(CultureInfo.InvariantCulture);
            LiveValue.IsUncommitted = _current != StartingValue;
        };
    }

    private string Preview(NumberChange change)
    {
        var (result, beyond) = Apply(_current, change);
        var shown = $"{change} → {result}";
        return change.Kind switch
        {
            NumberChangeKind.Subtract when beyond > 0 => $"{shown}, {beyond} poniżej zera",
            NumberChangeKind.Add when beyond > 0 => $"{shown}, {beyond} ponad maksimum",
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
