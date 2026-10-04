using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Trimming at any character, ending in a single "…" set straight against the last letter kept:
/// "Różdżka magicznych poc…". The stock character ellipsis keeps a space that happens to fall just
/// before the cut ("Różdżka magicznych …"), which reads as a word cut off rather than a name
/// shortened; here such a space goes with the rest of the cut text.
/// </summary>
public sealed class TightCharacterEllipsis : TextTrimming
{
    private const string Ellipsis = "…";

    private TightCharacterEllipsis()
    {
    }

    /// <summary>The trimming to set on a text block, as <see cref="TextTrimming.CharacterEllipsis"/> is.</summary>
    public static TextTrimming Instance { get; } = new TightCharacterEllipsis();

    public override TextCollapsingProperties CreateCollapsingProperties(TextCollapsingCreateInfo createInfo) =>
        new Collapsing(createInfo.Width, createInfo.TextRunProperties, createInfo.FlowDirection);

    public override string ToString() => nameof(TightCharacterEllipsis);

    /// <summary>
    /// Measures the cut itself rather than adjusting the stock ellipsis' result: the stock collapse
    /// consumes the line's shaped text, so the line cannot be split a second time after it.
    /// </summary>
    private sealed class Collapsing(double width, TextRunProperties properties, FlowDirection flowDirection) : TextCollapsingProperties
    {
        public override double Width => width;

        public override TextRun Symbol { get; } = new TextCharacters(Ellipsis, properties);

        public override FlowDirection FlowDirection => flowDirection;

        public override TextRun[]? Collapse(TextLine textLine)
        {
            if (textLine.WidthIncludingTrailingWhitespace <= Width)
            {
                return null;
            }

            var options = new TextShaperOptions(
                properties.Typeface.GlyphTypeface,
                properties.FontRenderingEmSize,
                (sbyte)(flowDirection == FlowDirection.RightToLeft ? 1 : 0),
                properties.CultureInfo);
            var symbol = new ShapedTextRun(TextShaper.Current.ShapeText(Ellipsis, options), properties);
            var available = Math.Max(0, Width - symbol.Size.Width);
            var start = textLine.FirstTextSourceIndex;

            // Every character that ends within the room left beside the ellipsis, whole clusters only.
            var hit = new CharacterHit(textLine.GetCharacterHitFromDistance(available).FirstCharacterIndex);
            while (hit.FirstCharacterIndex > start && textLine.GetDistanceFromCharacterHit(hit) > available)
            {
                hit = textLine.GetPreviousCaretCharacterHit(hit);
            }

            var text = string.Concat(textLine.TextRuns.Select(run => run.Text.ToString()));
            var kept = Math.Clamp(hit.FirstCharacterIndex - start, 0, text.Length);
            while (kept > 0 && char.IsWhiteSpace(text[kept - 1]))
            {
                kept--;
            }

            return CreateCollapsedRuns(textLine, kept, symbol);
        }
    }
}
