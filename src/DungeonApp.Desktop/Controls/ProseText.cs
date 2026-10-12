using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// A card's running text. A text block wraps lines but has no gap between a hard line break and the
/// line before it, so the text is shown as one selectable block per hard-break line, stacked a third
/// of a text line apart (DungeonSpacingXs): the line after a hard break starts a little lower than a
/// wrapped one, and much less low than a blank line would. The text may carry
/// <see cref="EmphasisMarkup"/>; an emphasized run that spans a break is emphasized on both sides.
/// <para>
/// Selecting across the blocks works through the enclosing <see cref="TextSelectionArea"/>, like
/// selecting across any two blocks of a card. The control carries no margin; its host spaces it.
/// </para>
/// </summary>
public sealed class ProseText : StackPanel
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ProseText, string?>(nameof(Text));

    /// <summary>The text blocks' style classes, separated by spaces.</summary>
    public static readonly StyledProperty<string> TextClassesProperty =
        AvaloniaProperty.Register<ProseText, string>(nameof(TextClasses), "body secondary");

    /// <summary>Whether the text is read as <see cref="EmphasisMarkup"/>; off, it is shown as written.</summary>
    public static readonly StyledProperty<bool> UsesMarkupProperty =
        AvaloniaProperty.Register<ProseText, bool>(nameof(UsesMarkup), true);

    private IReadOnlyList<EmphasisRun>? _runs;

    public ProseText()
    {
        Bind(SpacingProperty, this.GetResourceObservable("DungeonSpacingXs"));
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string TextClasses
    {
        get => GetValue(TextClassesProperty);
        set => SetValue(TextClassesProperty, value);
    }

    public bool UsesMarkup
    {
        get => GetValue(UsesMarkupProperty);
        set => SetValue(UsesMarkupProperty, value);
    }

    /// <summary>The blocks, in order, one per hard-break line.</summary>
    public IReadOnlyList<SelectableTextBlock> Blocks
    {
        get
        {
            var blocks = new List<SelectableTextBlock>();
            foreach (var child in Children)
            {
                if (child is SelectableTextBlock block)
                {
                    blocks.Add(block);
                }
            }

            return blocks;
        }
    }

    /// <summary>Shows already parsed runs (a cut of a longer text) instead of <see cref="Text"/>.</summary>
    public void SetRuns(IReadOnlyList<EmphasisRun> runs)
    {
        _runs = runs;
        Rebuild();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty || change.Property == UsesMarkupProperty)
        {
            _runs = null;
            Rebuild();
        }
        else if (change.Property == TextClassesProperty)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        Children.Clear();

        var runs = _runs;
        if (runs is null)
        {
            var text = Text ?? string.Empty;
            runs = UsesMarkup ? EmphasisMarkup.Parse(text) : [new EmphasisRun(text, IsEmphasized: false)];
        }

        foreach (var line in EmphasisMarkup.SplitLines(runs))
        {
            var block = new SelectableTextBlock { TextWrapping = TextWrapping.Wrap };
            foreach (var name in TextClasses.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                block.Classes.Add(name);
            }

            block.Inlines = EmphasisMarkup.ToInlines(line);
            Children.Add(block);
        }
    }
}
