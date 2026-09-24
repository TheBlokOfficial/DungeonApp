using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

/// <summary>
/// A gallery color swatch whose caption shows the value the swatch really paints. The value is read
/// from the brush itself, not written next to it, so the caption cannot drift from Tokens.axaml.
/// A theme brush takes its color from a DynamicResource, so the sample listens to the brush's colors
/// while it is on screen - it only redraws its own caption, nothing else.
/// </summary>
public partial class ColorSample : UserControl
{
    public static readonly StyledProperty<IBrush?> SwatchProperty =
        AvaloniaProperty.Register<ColorSample, IBrush?>(nameof(Swatch));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ColorSample, string?>(nameof(Title));

    public static readonly StyledProperty<string?> MeaningProperty =
        AvaloniaProperty.Register<ColorSample, string?>(nameof(Meaning));

    private readonly List<AvaloniaObject> _watched = [];

    public ColorSample()
    {
        InitializeComponent();
    }

    public IBrush? Swatch
    {
        get => GetValue(SwatchProperty);
        set => SetValue(SwatchProperty, value);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Meaning
    {
        get => GetValue(MeaningProperty);
        set => SetValue(MeaningProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TitleProperty)
        {
            TitleText.Text = Title;
        }
        else if (change.Property == MeaningProperty)
        {
            MeaningRun.Text = Meaning;
        }
        else if (change.Property == SwatchProperty)
        {
            SwatchBorder.Background = Swatch;
            if (VisualRoot is not null)
            {
                Unwatch();
                Watch();
            }

            ShowValue();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Watch();
        ShowValue();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // Theme brushes outlive the gallery; a handler left on them would keep this view alive.
        Unwatch();
        base.OnDetachedFromVisualTree(e);
    }

    private void Watch()
    {
        IEnumerable<AvaloniaObject> sources = Swatch switch
        {
            SolidColorBrush solid => [solid],
            GradientBrush gradient => gradient.GradientStops,
            _ => [],
        };

        foreach (var source in sources)
        {
            source.PropertyChanged += OnSwatchColorChanged;
            _watched.Add(source);
        }
    }

    private void Unwatch()
    {
        foreach (var source in _watched)
        {
            source.PropertyChanged -= OnSwatchColorChanged;
        }

        _watched.Clear();
    }

    private void OnSwatchColorChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == SolidColorBrush.ColorProperty || e.Property == GradientStop.ColorProperty)
        {
            ShowValue();
        }
    }

    private void ShowValue()
    {
        ValueRun.Text = Swatch switch
        {
            ISolidColorBrush solid => ToHex(solid.Color),
            IGradientBrush gradient => string.Join(" → ", gradient.GradientStops.Select(stop => ToHex(stop.Color))),
            null => string.Empty,
            _ => Swatch.GetType().Name,
        };
    }

    private static string ToHex(Color color) =>
        color.A == byte.MaxValue
            ? string.Create(CultureInfo.InvariantCulture, $"#{color.R:X2}{color.G:X2}{color.B:X2}")
            : string.Create(CultureInfo.InvariantCulture, $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}");
}
