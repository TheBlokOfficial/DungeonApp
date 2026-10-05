using System;
using System.IO;
using System.Linq;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using DungeonApp.Desktop.Entries;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The SVG icon reader: it draws a vector (no screen needed, unlike a bitmap), and refuses what it
/// would draw wrongly instead of guessing.
/// </summary>
public sealed class SvgIconTests
{
    private const string Square =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><path d="M0 0h512v512H0z"/></svg>""";

    [AvaloniaFact]
    public void A_path_with_a_viewBox_is_read_as_a_filled_shape()
    {
        var icon = SvgIcon.Parse(Square);

        Assert.Equal(new Avalonia.Rect(0, 0, 512, 512), icon.Viewbox);
        var group = Assert.IsType<DrawingGroup>(icon.Drawing);
        var shape = Assert.IsType<GeometryDrawing>(Assert.Single(group.Children));
        Assert.True(shape.Brush is ISolidColorBrush { Color.A: > 0 });
        Assert.Equal(512, shape.Geometry!.Bounds.Width);
    }

    [AvaloniaFact]
    public void Groups_without_attributes_and_descriptions_are_allowed_and_evenodd_is_kept()
    {
        var icon = SvgIcon.Parse(
            """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><title>x</title><g><path fill-rule="evenodd" d="M0 0h4v4H0z"/><path d="M5 5h4v4H5z"/></g></svg>""");

        var group = Assert.IsType<DrawingGroup>(icon.Drawing);
        Assert.Equal(2, group.Children.Count);
        Assert.All(group.Children, child => Assert.IsType<GeometryDrawing>(child));
    }

    [AvaloniaTheory]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><circle r="4"/></svg>""")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><path d="M0 0h4v4z" transform="scale(2)"/></svg>""")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><g transform="scale(2)"><path d="M0 0h4v4z"/></g></svg>""")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><path style="fill:red" d="M0 0h4v4z"/></svg>""")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg"><path d="M0 0h4v4z"/></svg>""")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"/>""")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 0 10"><path d="M0 0h4v4z"/></svg>""")]
    [InlineData("""<html/>""")]
    [InlineData("not xml")]
    [InlineData("""<!DOCTYPE svg [<!ENTITY a "b">]><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><path d="M0 0h4v4z"/></svg>""")]
    public void An_svg_outside_the_subset_is_refused(string svg)
    {
        Assert.Throws<FormatException>(() => SvgIcon.Parse(svg));
    }

    [AvaloniaFact]
    public void Every_condition_icon_of_the_shipped_pack_is_read_with_a_shape()
    {
        var folder = Directory.EnumerateDirectories(AppContext.BaseDirectory, "conditions", SearchOption.AllDirectories)
            .FirstOrDefault(path => path.Replace('\\', '/').EndsWith("dnd5e-srd/images/conditions", StringComparison.Ordinal));
        Assert.NotNull(folder);

        var files = Directory.GetFiles(folder, "*.svg");
        Assert.Equal(15, files.Length);

        foreach (var file in files)
        {
            var icon = SvgIcon.Load(file);
            var bounds = icon.Drawing!.GetBounds();
            Assert.True(bounds.Width > 100 && bounds.Height > 100, file);
        }
    }
}
