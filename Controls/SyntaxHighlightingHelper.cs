using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using DungeonApp.Models.Campaigns.Engine.Commands;

namespace DungeonApp.Controls;

public class SyntaxHighlightingHelper
{
    public static readonly AttachedProperty<IEnumerable<ParsedToken>> ParsedTokensProperty =
        AvaloniaProperty.RegisterAttached<SyntaxHighlightingHelper, TextBlock, IEnumerable<ParsedToken>>(
            "ParsedTokens");

    static SyntaxHighlightingHelper()
    {
        ParsedTokensProperty.Changed.AddClassHandler<TextBlock>(OnParsedTokensChanged);
    }

    private static readonly Dictionary<string, IBrush> _brushCache = new();

    public static IEnumerable<ParsedToken> GetParsedTokens(AvaloniaObject element)
    {
        return element.GetValue(ParsedTokensProperty);
    }

    public static void SetParsedTokens(AvaloniaObject element, IEnumerable<ParsedToken> value)
    {
        element.SetValue(ParsedTokensProperty, value);
    }

    private static void OnParsedTokensChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        textBlock.Inlines?.Clear();

        if (e.NewValue is IEnumerable<ParsedToken> tokens)
        {
            if (textBlock.Inlines == null)
            {
                textBlock.Inlines = new InlineCollection();
            }

            foreach (var token in tokens)
            {
                var run = new Run
                {
                    Text = token.DisplayText
                };

                if (!_brushCache.TryGetValue(token.Color, out var brush))
                {
                    try
                    {
                        brush = Brush.Parse(token.Color);
                        _brushCache[token.Color] = brush;
                    }
                    catch
                    {
                        // Fallback jeśli kolor jest nieprawidłowy, nie cache'ujemy błędu żeby ew. się poprawiło,
                        // albo po prostu ignorujemy. Dla bezpieczeństwa cache'ujemy np. przezroczystość lub pomijamy.
                        brush = Brushes.Transparent;
                        _brushCache[token.Color] = brush;
                    }
                }

                if (brush != Brushes.Transparent)
                {
                    run.Foreground = brush;
                }
                
                textBlock.Inlines.Add(run);
            }
        }
    }
}
