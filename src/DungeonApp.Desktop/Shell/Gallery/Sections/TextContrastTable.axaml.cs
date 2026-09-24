using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

/// <summary>
/// Every text color on every background it stands on, each with its WCAG contrast ratio computed
/// from the current color resources. The rule (docs/decisions.md, "Niezmiennik interfejsu") is at
/// least 4.5:1; a ratio below it is written in the danger-text color.
/// </summary>
public partial class TextContrastTable : UserControl
{
    /// <summary>WCAG AA for normal-size text.</summary>
    private const double MinimumContrast = 4.5;

    private const double CellWidth = 176;
    private const double RowHeaderWidth = 128;

    private static readonly CultureInfo NumberCulture = CultureInfo.GetCultureInfo("pl-PL");

    private static readonly (string ColorKey, string Name, string Token)[] TextRoles =
    [
        ("DungeonTextPrimaryColor", "podstawowy", "TextPrimary"),
        ("DungeonTextRegularColor", "drugorzędny", "TextRegular"),
        ("DungeonTextMutedColor", "przygaszony", "TextMuted"),
        ("DungeonAccentColor", "akcent", "Accent"),
        ("DungeonDangerTextColor", "niebezpieczeństwo", "DangerText"),
        ("DungeonSuccessColor", "sukces", "Success"),
        ("DungeonWarningColor", "ostrzeżenie", "Warning"),
    ];

    private static readonly (string ColorKey, string Name, string Token)[] Backgrounds =
    [
        ("DungeonBackstageColor", "zaplecze", "Backstage"),
        ("DungeonFrameColor", "rama", "Frame"),
        ("DungeonBackstageCardColor", "karta", "BackstageCard"),
        ("DungeonBackstageRowColor", "wiersz", "BackstageRow"),
    ];

    private readonly List<Cell> _cells = [];

    public TextContrastTable()
    {
        InitializeComponent();
        BuildTable();
        ActualThemeVariantChanged += (_, _) => Refresh();
        ResourcesChanged += (_, _) => Refresh();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Refresh();
    }

    /// <summary>WCAG 2 contrast ratio of two opaque colors: (L1 + 0.05) / (L2 + 0.05), L1 the lighter.</summary>
    private static double ContrastRatio(Color first, Color second)
    {
        var a = RelativeLuminance(first);
        var b = RelativeLuminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double RelativeLuminance(Color color) =>
        (0.2126 * Linear(color.R)) + (0.7152 * Linear(color.G)) + (0.0722 * Linear(color.B));

    private static double Linear(byte channel)
    {
        var c = channel / 255.0;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    private void BuildTable()
    {
        Table.ColumnDefinitions.Add(new ColumnDefinition(RowHeaderWidth, GridUnitType.Pixel));
        foreach (var _ in Backgrounds)
        {
            Table.ColumnDefinitions.Add(new ColumnDefinition(CellWidth, GridUnitType.Pixel));
        }

        for (var row = 0; row <= TextRoles.Length; row++)
        {
            Table.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        for (var column = 0; column < Backgrounds.Length; column++)
        {
            AddHeader(Backgrounds[column].Name, Backgrounds[column].Token, row: 0, column: column + 1);
        }

        for (var row = 0; row < TextRoles.Length; row++)
        {
            AddHeader(TextRoles[row].Name, TextRoles[row].Token, row: row + 1, column: 0);
            for (var column = 0; column < Backgrounds.Length; column++)
            {
                AddCell(TextRoles[row].ColorKey, Backgrounds[column].ColorKey, row + 1, column + 1);
            }
        }
    }

    private void AddHeader(string name, string token, int row, int column)
    {
        var header = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        header.Children.Add(Classed(new TextBlock { Text = name }, "label"));
        header.Children.Add(Classed(new TextBlock { Text = token }, "caption", "muted"));
        Grid.SetRow(header, row);
        Grid.SetColumn(header, column);
        Table.Children.Add(header);
    }

    private void AddCell(string textColorKey, string backgroundColorKey, int row, int column)
    {
        var sample = Classed(new TextBlock { Text = "Aa Goblin 12", VerticalAlignment = VerticalAlignment.Center }, "label");
        var ratio = Classed(new TextBlock { VerticalAlignment = VerticalAlignment.Center }, "mono");
        Grid.SetColumn(ratio, 1);

        var content = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        content.Bind(Grid.ColumnSpacingProperty, this.GetResourceObservable("DungeonSpacingSm"));
        content.Children.Add(sample);
        content.Children.Add(ratio);

        var border = new Border { Child = content };
        border.Bind(Border.PaddingProperty, this.GetResourceObservable("DungeonPaddingMd"));
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        Table.Children.Add(border);

        _cells.Add(new Cell(textColorKey, backgroundColorKey, border, sample, ratio));
    }

    private void Refresh()
    {
        if (!TryGetColor("DungeonTextPrimaryColor", out var passing)
            || !TryGetColor("DungeonDangerTextColor", out var failing))
        {
            return;
        }

        foreach (var cell in _cells)
        {
            if (!TryGetColor(cell.TextColorKey, out var text) || !TryGetColor(cell.BackgroundColorKey, out var background))
            {
                continue;
            }

            var contrast = ContrastRatio(text, background);
            cell.Border.Background = new SolidColorBrush(background);
            cell.Sample.Foreground = new SolidColorBrush(text);
            cell.Ratio.Text = contrast.ToString("0.0", NumberCulture);
            cell.Ratio.Foreground = new SolidColorBrush(contrast < MinimumContrast ? failing : passing);
        }
    }

    private bool TryGetColor(string key, out Color color)
    {
        if (this.TryFindResource(key, ActualThemeVariant, out var value) && value is Color found)
        {
            color = found;
            return true;
        }

        color = default;
        return false;
    }

    private static T Classed<T>(T element, params string[] classes)
        where T : StyledElement
    {
        element.Classes.AddRange(classes);
        return element;
    }

    private sealed record Cell(string TextColorKey, string BackgroundColorKey, Border Border, TextBlock Sample, TextBlock Ratio);
}
