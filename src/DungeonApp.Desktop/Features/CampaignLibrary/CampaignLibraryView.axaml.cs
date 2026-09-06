using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Features.CampaignLibrary;

public partial class CampaignLibraryView : UserControl
{
    // Below this width there is no appreciable unused backstage area, so the page intentionally
    // remains a flat surface. This measures the content area, not the whole native window.
    private const double WideBackstageThreshold = 1440;

    private TopLevel? _topLevel;

    public CampaignLibraryView()
    {
        InitializeComponent();
        BackstageSurface.SizeChanged += OnBackstageSurfaceSizeChanged;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(InputElement.PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
        UpdateBackstageTreatment(BackstageSurface.Bounds.Width);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(InputElement.PointerPressedEvent, OnPreviewPointerPressed);
        _topLevel = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_topLevel is null || !e.GetCurrentPoint(_topLevel).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (_topLevel.FocusManager.GetFocusedElement() is not TextBox focusedTextBox)
        {
            return;
        }

        // Keep editing when the press is still inside the focused field. Any press elsewhere clears
        // the edit focus before its actual target handles the event; the event remains unhandled, so
        // a button or another input can then receive focus and act normally.
        if (e.Source is Avalonia.Visual source &&
            (ReferenceEquals(source, focusedTextBox) ||
             ReferenceEquals(source.FindAncestorOfType<TextBox>(), focusedTextBox)))
        {
            return;
        }

        // Avalonia 12.0.5 clears focus by focusing a null element. The dedicated ClearFocus API is
        // newer; keep this call explicit until the framework upgrade makes that API available.
        _topLevel.FocusManager.Focus(null, NavigationMethod.Pointer, e.KeyModifiers);
    }

    private void OnBackstageSurfaceSizeChanged(object? sender, SizeChangedEventArgs e) =>
        UpdateBackstageTreatment(e.NewSize.Width);

    private void UpdateBackstageTreatment(double width)
    {
        const string wideBackstageClass = "wide-backstage";
        var shouldUseGradient = width >= WideBackstageThreshold;

        if (shouldUseGradient && !BackstageSurface.Classes.Contains(wideBackstageClass))
        {
            BackstageSurface.Classes.Add(wideBackstageClass);
        }
        else if (!shouldUseGradient)
        {
            BackstageSurface.Classes.Remove(wideBackstageClass);
        }
    }
}
