using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// One piece of a text written with <see cref="EmphasisMarkup"/>: a run of plain text or a run the
/// author emphasized.
/// </summary>
public readonly record struct EmphasisRun(string Text, bool IsEmphasized);

/// <summary>
/// The markup a pack's prose may carry. <c>**…**</c> marks a run the reader's eye should land on (an
/// attack bonus, a damage roll, a save DC); the application never finds such runs by itself - only
/// what the author marked is emphasized. <c>[[id|text]]</c> names another entry: the card shows only
/// the text. The id is in the pack so that a later link needs no second pass over the prose.
/// Nothing else is markup.
/// <para>
/// A <c>**</c> opens a run and the next <c>**</c> closes it. A <c>**</c> left without a partner, and a
/// pair with nothing between them, stay in the text as written: a stray pair of asterisks is
/// something to see and fix, not a reason for the rest of the paragraph to turn bold. A reference
/// without an id or a text stays as written for the same reason.
/// </para>
/// <para>
/// <see cref="TextProperty"/> puts the parsed runs into a <see cref="TextBlock"/> (a
/// <see cref="SelectableTextBlock"/> too) as inlines: plain runs take the text block's own look, an
/// emphasized run gets the <c>emphasis</c> class (Typography.axaml).
/// </para>
/// </summary>
public static class EmphasisMarkup
{
    private const string Marker = "**";

    private static readonly Regex Reference = new(@"\[\[[^\[\]|]+\|([^\[\]|]+)\]\]", RegexOptions.CultureInvariant);

    public static readonly AttachedProperty<string?> TextProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, string?>("Text", typeof(EmphasisMarkup));

    static EmphasisMarkup()
    {
        TextProperty.Changed.AddClassHandler<TextBlock>((block, change) => Apply(block, change.GetNewValue<string?>()));
    }

    public static string? GetText(TextBlock block) => block.GetValue(TextProperty);

    public static void SetText(TextBlock block, string? value) => block.SetValue(TextProperty, value);

    /// <summary>Splits <paramref name="text"/> into plain and emphasized runs, in order; empty runs are left out.</summary>
    public static IReadOnlyList<EmphasisRun> Parse(string text)
    {
        text = Reference.Replace(text, "$1");
        var runs = new List<EmphasisRun>();
        var plainStart = 0;
        var position = 0;

        while (position < text.Length)
        {
            var open = text.IndexOf(Marker, position, System.StringComparison.Ordinal);
            if (open < 0)
            {
                break;
            }

            var close = text.IndexOf(Marker, open + Marker.Length, System.StringComparison.Ordinal);
            if (close < 0)
            {
                break;
            }

            if (close == open + Marker.Length)
            {
                // "****": a pair around nothing - kept as text, and the search goes on after it.
                position = close + Marker.Length;
                continue;
            }

            AddPlain(runs, text, plainStart, open);
            runs.Add(new EmphasisRun(text[(open + Marker.Length)..close], IsEmphasized: true));
            plainStart = close + Marker.Length;
            position = plainStart;
        }

        AddPlain(runs, text, plainStart, text.Length);
        return runs;
    }

    private static void AddPlain(List<EmphasisRun> runs, string text, int start, int end)
    {
        if (end > start)
        {
            runs.Add(new EmphasisRun(text[start..end], IsEmphasized: false));
        }
    }

    private static void Apply(TextBlock block, string? text)
    {
        var inlines = new InlineCollection();

        foreach (var run in Parse(text ?? string.Empty))
        {
            var inline = new Run(run.Text);
            if (run.IsEmphasized)
            {
                inline.Classes.Add("emphasis");
            }

            inlines.Add(inline);
        }

        block.Inlines = inlines;
    }
}
