using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;

namespace DungeonApp.Desktop.Entries;

/// <summary>
/// Reads a one-colour icon from an SVG file into a <see cref="DrawingImage"/>. Deliberately a small
/// subset - the root <c>svg</c> with a <c>viewBox</c>, <c>path</c> elements with a <c>d</c> (and
/// <c>fill-rule</c>), optionally wrapped in attribute-less <c>g</c> - because the picture is only a
/// shape: the frame paints it in the theme's colour, so colours, transforms, styles and clipping have
/// nothing to say. Anything else is refused with a reason instead of being drawn wrongly; the caller
/// shows a refused file as an unreadable picture. DTDs are prohibited, so the file cannot pull in
/// anything from outside.
/// </summary>
public static class SvgIcon
{
    private static readonly HashSet<string> Ignored = ["title", "desc", "metadata"];

    /// <summary>Reads the file at <paramref name="path"/>; throws <see cref="FormatException"/> for an SVG outside the subset.</summary>
    public static DrawingImage Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var stream = File.OpenRead(path);
        return Read(XmlReader.Create(stream, Settings()));
    }

    /// <summary>Reads SVG text; see <see cref="Load"/>.</summary>
    public static DrawingImage Parse(string svg)
    {
        ArgumentNullException.ThrowIfNull(svg);

        return Read(XmlReader.Create(new StringReader(svg), Settings()));
    }

    private static XmlReaderSettings Settings() => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
    };

    private static DrawingImage Read(XmlReader reader)
    {
        XElement root;

        using (reader)
        {
            try
            {
                root = XDocument.Load(reader).Root!;
            }
            catch (XmlException ex)
            {
                throw new FormatException($"The SVG cannot be read as XML: {ex.Message}", ex);
            }
        }

        if (root.Name.LocalName != "svg")
        {
            throw new FormatException("The root element is not 'svg'.");
        }

        var viewBox = ParseViewBox((string?)root.Attribute("viewBox"));
        var shapes = new List<Drawing>();
        Collect(root, shapes);

        if (shapes.Count == 0)
        {
            throw new FormatException("The SVG has no path.");
        }

        var group = new DrawingGroup();

        foreach (var shape in shapes)
        {
            group.Children.Add(shape);
        }

        return new DrawingImage(group) { Viewbox = viewBox };
    }

    private static void Collect(XElement parent, List<Drawing> shapes)
    {
        foreach (var element in parent.Elements())
        {
            var name = element.Name.LocalName;

            if (Ignored.Contains(name))
            {
                continue;
            }

            switch (name)
            {
                case "path":
                    shapes.Add(ReadPath(element));
                    break;

                case "g":
                    if (element.HasAttributes)
                    {
                        throw new FormatException("A 'g' element with attributes is not supported.");
                    }

                    Collect(element, shapes);
                    break;

                default:
                    throw new FormatException($"The element '{name}' is not supported.");
            }
        }
    }

    private static GeometryDrawing ReadPath(XElement path)
    {
        string? data = null;
        var fillRule = FillRule.NonZero;

        foreach (var attribute in path.Attributes())
        {
            switch (attribute.Name.LocalName)
            {
                case "d":
                    data = attribute.Value;
                    break;

                case "fill-rule":
                    fillRule = attribute.Value switch
                    {
                        "evenodd" => FillRule.EvenOdd,
                        "nonzero" => FillRule.NonZero,
                        var other => throw new FormatException($"The fill-rule '{other}' is not supported."),
                    };
                    break;

                default:
                    throw new FormatException($"The path attribute '{attribute.Name.LocalName}' is not supported.");
            }
        }

        if (string.IsNullOrWhiteSpace(data))
        {
            throw new FormatException("A path has no 'd'.");
        }

        Geometry geometry;

        try
        {
            // The path mini-language carries the fill rule as its leading "F" command.
            geometry = Geometry.Parse((fillRule == FillRule.EvenOdd ? "F1 " : "F0 ") + data);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException)
        {
            throw new FormatException($"The path data cannot be read: {ex.Message}", ex);
        }

        // Opaque on purpose: the frame decides the colour, but a drawing whose brush paints nothing
        // is read as an outline and would be drawn hollow.
        return new GeometryDrawing { Geometry = geometry, Brush = Brushes.Black };
    }

    private static Rect ParseViewBox(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException("The 'svg' element has no viewBox.");
        }

        var parts = value.Split([' ', ',', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 4
            || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
            || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
            || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var width)
            || !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var height)
            || width <= 0
            || height <= 0)
        {
            throw new FormatException($"The viewBox '{value}' is not four numbers with a positive size.");
        }

        return new Rect(x, y, width, height);
    }
}
