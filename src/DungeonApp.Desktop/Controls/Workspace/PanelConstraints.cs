using System;

namespace DungeonApp.Desktop.Controls.Workspace;

/// <summary>
/// A panel's size contract, taken from its descriptor. <see cref="double.PositiveInfinity"/> is a
/// valid maximum and means "FluidData" in the UI contract's vocabulary.
/// </summary>
public readonly record struct PanelConstraints(
    double MinWidth,
    double MinHeight,
    double MaxWidth,
    double MaxHeight)
{
    /// <summary>
    /// A descriptor with Max below Min would make <see cref="Math.Clamp(double, double, double)"/>
    /// throw. Minimum wins: a panel that cannot show its minimum is useless.
    /// </summary>
    public double EffectiveMaxWidth => Math.Max(MinWidth, MaxWidth);

    public double EffectiveMaxHeight => Math.Max(MinHeight, MaxHeight);
}
