using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Layer over all window content (MainWindow.axaml, last child of the main panel): hosts notification
/// toasts (<see cref="NotificationToast"/>) at bottom right above the status bar, and an open confirmation dialog
/// (<see cref="ConfirmationDialog"/>) above everything. When empty has no background, so does not intercept the pointer - clicks
/// reach underlying content. Views do not know it: display methods locate it through the element
/// from which they were invoked (<see cref="Find"/>).
/// </summary>
public sealed class WindowOverlay : Panel
{
    public WindowOverlay()
    {
        Toasts = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        Toasts.Bind(StackPanel.SpacingProperty, Toasts.GetResourceObservable("DungeonSpacingSm"));
        Toasts.GetResourceObservable("DungeonPaddingLg").Subscribe(new AnonymousObserver<object?>(value =>
        {
            toastGap = value is Thickness gap ? gap : default;
            UpdateToastMargin();
        }));
        Toasts.GetResourceObservable("DungeonStatusBarHeight").Subscribe(new AnonymousObserver<object?>(value =>
        {
            statusBarHeight = value is GridLength height ? height.Value : 0;
            UpdateToastMargin();
        }));
        Children.Add(Toasts);
    }

    private Thickness toastGap;
    private double statusBarHeight;

    /// <summary>Toast stack: newest at the bottom.</summary>
    internal StackPanel Toasts { get; }

    /// <summary>Overlay of the window containing <paramref name="origin"/>; null if it has none.</summary>
    internal static WindowOverlay? Find(Visual origin) =>
        TopLevel.GetTopLevel(origin)?.GetVisualDescendants().OfType<WindowOverlay>().FirstOrDefault();

    /// <summary>
    /// Stack sits above the status bar (DungeonStatusBarHeight, Shell/AppShellView.axaml) with the same
    /// DungeonPaddingLg gap as from the window's right edge - toast does not cover the bar.
    /// </summary>
    private void UpdateToastMargin() =>
        Toasts.Margin = new Thickness(toastGap.Left, toastGap.Top, toastGap.Right, toastGap.Bottom + statusBarHeight);
}
