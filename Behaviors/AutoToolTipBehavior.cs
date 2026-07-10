using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Linq;

namespace DungeonApp.Behaviors;

public class AutoToolTipBehavior
{
    public static readonly AttachedProperty<bool> ShowOnTrimmedOnlyProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("ShowOnTrimmedOnly", typeof(AutoToolTipBehavior));

    public static void SetShowOnTrimmedOnly(AvaloniaObject element, bool value) => element.SetValue(ShowOnTrimmedOnlyProperty, value);
    public static bool GetShowOnTrimmedOnly(AvaloniaObject element) => element.GetValue(ShowOnTrimmedOnlyProperty);

    static AutoToolTipBehavior()
    {
        ShowOnTrimmedOnlyProperty.Changed.AddClassHandler<TextBlock>(HandleShowOnTrimmedOnlyChanged);
    }

    private static void HandleShowOnTrimmedOnlyChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            textBlock.PropertyChanged += TextBlock_PropertyChanged;
            UpdateToolTip(textBlock);
        }
        else
        {
            textBlock.PropertyChanged -= TextBlock_PropertyChanged;
            ToolTip.SetTip(textBlock, null);
        }
    }

    private static void TextBlock_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is TextBlock tb)
        {
            if (e.Property == TextBlock.BoundsProperty || e.Property == TextBlock.TextProperty)
            {
                UpdateToolTip(tb);
            }
        }
    }

    private static void UpdateToolTip(TextBlock tb)
    {
        Dispatcher.UIThread.Post(() => 
        {
            var isTrimmed = tb.TextTrimming != Avalonia.Media.TextTrimming.None && 
                            tb.TextLayout?.TextLines.Any(x => x.HasCollapsed) == true;
                            
            if (isTrimmed)
            {
                ToolTip.SetTip(tb, tb.Text);
            }
            else
            {
                ToolTip.SetTip(tb, null);
            }
        }, DispatcherPriority.Background);
    }
}
