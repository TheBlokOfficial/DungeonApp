using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace DungeonApp.Views.Sessions;

public partial class SessionNotesView : UserControl
{
    // Tracks the most recently focused TextBox to apply formatting to the correct one (new note vs existing note).
    private TextBox? _lastFocusedTextBox;
    
    // Stores the selection snapshot per TextBox (by reference since existing notes don't have unique names)
    private readonly Dictionary<TextBox, (int Start, int End)> _savedSelections = new();

    public SessionNotesView()
    {
        InitializeComponent();
        
        // Add a tunneling PointerPressed handler to catch clicks on toolbar buttons
        // BEFORE the Button handles the event and potentially before focus is lost.
        this.AddHandler(InputElement.PointerPressedEvent, OnGlobalPointerPressed, RoutingStrategies.Tunnel);
        
        // Also track focus changes (e.g. via Tab key)
        this.AddHandler(InputElement.GotFocusEvent, OnGlobalGotFocus, RoutingStrategies.Bubble);
    }

    private void OnGlobalGotFocus(object? sender, RoutedEventArgs e)
    {
        var visual = e.Source as Avalonia.Visual;
        var clickedTextBox = visual as TextBox ?? visual?.FindAncestorOfType<TextBox>();
        if (clickedTextBox != null)
        {
            _lastFocusedTextBox = clickedTextBox;
        }
    }

    private void OnGlobalPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var visual = e.Source as Avalonia.Visual;
        
        // Track whichever TextBox the user clicks on (or inside)
        var clickedTextBox = visual as TextBox ?? visual?.FindAncestorOfType<TextBox>();
        if (clickedTextBox != null)
        {
            _lastFocusedTextBox = clickedTextBox;
        }

        var button = visual as Button ?? visual?.FindAncestorOfType<Button>();
        
        if (button != null && button.Tag is string tag && tag.StartsWith("Format:"))
        {
            var textBox = _lastFocusedTextBox ?? this.FindControl<TextBox>("NewNoteEditor");
            if (textBox != null)
            {
                var start = Math.Min(textBox.SelectionStart, textBox.SelectionEnd);
                var end = Math.Max(textBox.SelectionStart, textBox.SelectionEnd);
                _savedSelections[textBox] = (start, end);
            }
        }
    }

    private void InsertWrapFormatting(TextBox textBox, string syntax, (int Start, int End) sel)
    {
        var text = textBox.Text ?? string.Empty;
        var selStart = sel.Start;
        var selEnd = Math.Min(sel.End, text.Length);
        var selLength = Math.Max(0, selEnd - selStart);

        if (selLength > 0)
        {
            var selected = text.Substring(selStart, selLength);
            var replacement = syntax + selected + syntax;
            textBox.Text = text.Remove(selStart, selLength).Insert(selStart, replacement);
            textBox.SelectionStart = selStart + syntax.Length;
            textBox.SelectionEnd = selStart + syntax.Length + selLength;
        }
        else
        {
            textBox.Text = text.Insert(selStart, syntax + syntax);
            textBox.SelectionStart = selStart + syntax.Length;
            textBox.SelectionEnd = selStart + syntax.Length;
        }

        textBox.Focus();
    }

    private void InsertLinePrefixFormatting(TextBox textBox, string prefix, (int Start, int End) sel)
    {
        var text = textBox.Text ?? string.Empty;
        var selStart = Math.Min(sel.Start, text.Length);

        var lineStart = text.LastIndexOf('\n', Math.Max(0, selStart - 1));
        lineStart = lineStart < 0 ? 0 : lineStart + 1;

        textBox.Text = text.Insert(lineStart, prefix);
        textBox.SelectionStart = selStart + prefix.Length;
        textBox.SelectionEnd = selStart + prefix.Length;
        textBox.Focus();
    }

    private void InsertBlockFormatting(TextBox textBox, string block, (int Start, int End) sel)
    {
        var text = textBox.Text ?? string.Empty;
        var selStart = Math.Min(sel.Start, text.Length);

        var insert = string.Empty;
        if (selStart > 0 && text[selStart - 1] != '\n')
            insert += "\n";
        insert += block + "\n";

        textBox.Text = text.Insert(selStart, insert);
        textBox.SelectionStart = selStart + insert.Length;
        textBox.SelectionEnd = selStart + insert.Length;
        textBox.Focus();
    }

    public void OnFormatButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag) return;

        var parts = tag.Split(':', 2);
        if (parts.Length != 2 || parts[0] != "Format") return;

        var formatType = parts[1];

        var textBox = _lastFocusedTextBox ?? this.FindControl<TextBox>("NewNoteEditor");
        if (textBox == null) return;

        var sel = _savedSelections.TryGetValue(textBox, out var saved)
            ? saved
            : (textBox.SelectionStart, textBox.SelectionEnd);

        switch (formatType)
        {
            case "bold":
                InsertWrapFormatting(textBox, "**", sel);
                break;
            case "italic":
                InsertWrapFormatting(textBox, "*", sel);
                break;
            case "strikethrough":
                InsertWrapFormatting(textBox, "~~", sel);
                break;
            case "code":
                InsertWrapFormatting(textBox, "`", sel);
                break;
            case "heading":
                InsertLinePrefixFormatting(textBox, "## ", sel);
                break;
            case "bullet":
                InsertLinePrefixFormatting(textBox, "- ", sel);
                break;
            case "numbered":
                InsertLinePrefixFormatting(textBox, "1. ", sel);
                break;
            case "hr":
                InsertBlockFormatting(textBox, "---", sel);
                break;
        }

        _savedSelections.Remove(textBox);
    }
}
