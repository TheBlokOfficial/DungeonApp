using System;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace DungeonApp.Controls;

/// <summary>
/// A lightweight Markdown renderer that supports basic inline formatting
/// (bold, italic, strikethrough, code) and block elements (headings, lists, horizontal rules).
/// Built as a replacement for Markdown.Avalonia which is incompatible with Avalonia 12.
/// </summary>
public class SimpleMarkdownViewer : StackPanel
{
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<SimpleMarkdownViewer, string?>(nameof(Markdown));

    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    private static readonly Regex InlinePattern = new(
        @"(\*\*(.+?)\*\*" +        // **bold**
        @"|(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)" +  // *italic*
        @"|~~(.+?)~~" +              // ~~strikethrough~~
        @"|`(.+?)`)",               // `code`
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex NumberedListPattern = new(
        @"^(\d+\.)\s(.*)", RegexOptions.Compiled);

    static SimpleMarkdownViewer()
    {
        MarkdownProperty.Changed.AddClassHandler<SimpleMarkdownViewer>((viewer, _) => viewer.Render());
    }

    public SimpleMarkdownViewer()
    {
        Spacing = 2;
    }

    private void Render()
    {
        Children.Clear();
        var text = Markdown;
        if (string.IsNullOrEmpty(text)) return;

        var lines = text.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.TrimEnd('\r');

            // Horizontal rule
            if (trimmed is "---" or "***" or "___")
            {
                Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.Parse("#333333")),
                    Margin = new Thickness(0, 8)
                });
                continue;
            }

            // Heading H1
            if (trimmed.StartsWith("# ") && !trimmed.StartsWith("## "))
            {
                var tb = CreateFormattedTextBlock(trimmed[2..]);
                tb.FontSize = 22;
                tb.FontWeight = FontWeight.Bold;
                tb.Margin = new Thickness(0, 8, 0, 4);
                Children.Add(tb);
                continue;
            }

            // Heading H2
            if (trimmed.StartsWith("## ") && !trimmed.StartsWith("### "))
            {
                var tb = CreateFormattedTextBlock(trimmed[3..]);
                tb.FontSize = 18;
                tb.FontWeight = FontWeight.Bold;
                tb.Margin = new Thickness(0, 6, 0, 4);
                Children.Add(tb);
                continue;
            }

            // Heading H3
            if (trimmed.StartsWith("### "))
            {
                var tb = CreateFormattedTextBlock(trimmed[4..]);
                tb.FontSize = 16;
                tb.FontWeight = FontWeight.SemiBold;
                tb.Margin = new Thickness(0, 4, 0, 2);
                Children.Add(tb);
                continue;
            }

            // Bullet list
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal };
                panel.Children.Add(new TextBlock
                {
                    Text = "•  ",
                    Foreground = new SolidColorBrush(Color.Parse("#D4A017")),
                    VerticalAlignment = VerticalAlignment.Top,
                    FontSize = 15
                });
                panel.Children.Add(CreateFormattedTextBlock(trimmed[2..]));
                Children.Add(panel);
                continue;
            }

            // Numbered list
            var numMatch = NumberedListPattern.Match(trimmed);
            if (numMatch.Success)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal };
                panel.Children.Add(new TextBlock
                {
                    Text = numMatch.Groups[1].Value + "  ",
                    Foreground = new SolidColorBrush(Color.Parse("#D4A017")),
                    VerticalAlignment = VerticalAlignment.Top,
                    FontSize = 15
                });
                panel.Children.Add(CreateFormattedTextBlock(numMatch.Groups[2].Value));
                Children.Add(panel);
                continue;
            }

            // Empty line = paragraph break
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                Children.Add(new Border { Height = 8 });
                continue;
            }

            // Regular paragraph
            Children.Add(CreateFormattedTextBlock(trimmed));
        }
    }

    private static TextBlock CreateFormattedTextBlock(string text)
    {
        var tb = new TextBlock
        {
            FontSize = 15,
            Foreground = new SolidColorBrush(Color.Parse("#E5E5E5")),
            TextWrapping = TextWrapping.Wrap
        };

        ParseInlines(tb, text);
        return tb;
    }

    private static void ParseInlines(TextBlock tb, string text)
    {
        if (tb.Inlines != null)
        {
            foreach (var inline in ParseInlinesRecursive(text))
            {
                tb.Inlines.Add(inline);
            }
        }
    }

    private static System.Collections.Generic.IEnumerable<Inline> ParseInlinesRecursive(string text)
    {
        var matches = InlinePattern.Matches(text);

        if (matches.Count == 0)
        {
            yield return new Run(text);
            yield break;
        }

        int lastIndex = 0;

        foreach (Match match in matches)
        {
            // Text before this match
            if (match.Index > lastIndex)
            {
                yield return new Run(text[lastIndex..match.Index]);
            }

            var span = new Span();

            if (match.Groups[2].Success) // **bold**
            {
                span.FontWeight = FontWeight.Bold;
                foreach (var child in ParseInlinesRecursive(match.Groups[2].Value))
                {
                    span.Inlines.Add(child);
                }
            }
            else if (match.Groups[3].Success) // *italic*
            {
                span.FontStyle = FontStyle.Italic;
                foreach (var child in ParseInlinesRecursive(match.Groups[3].Value))
                {
                    span.Inlines.Add(child);
                }
            }
            else if (match.Groups[4].Success) // ~~strikethrough~~
            {
                span.TextDecorations = TextDecorations.Strikethrough;
                foreach (var child in ParseInlinesRecursive(match.Groups[4].Value))
                {
                    span.Inlines.Add(child);
                }
            }
            else if (match.Groups[5].Success) // `code`
            {
                span.FontFamily = new FontFamily("Consolas, 'Courier New', monospace");
                span.Foreground = new SolidColorBrush(Color.Parse("#D4A017"));
                foreach (var child in ParseInlinesRecursive(match.Groups[5].Value))
                {
                    span.Inlines.Add(child);
                }
            }

            yield return span;
            lastIndex = match.Index + match.Length;
        }

        // Remaining text after last match
        if (lastIndex < text.Length)
        {
            yield return new Run(text[lastIndex..]);
        }
    }
}
