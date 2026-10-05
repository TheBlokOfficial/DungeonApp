using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace DungeonApp.Desktop.Controls;

/// <summary>Notification variant by meaning.</summary>
public enum NotificationKind
{
    /// <summary>Information: carries no state; auto-dismisses.</summary>
    Information,

    /// <summary>Warning: auto-dismisses.</summary>
    Warning,

    /// <summary>Error: stays until dismissed, body is selectable.</summary>
    Error,
}

/// <summary>
/// Notification: toast at the window's bottom right (stack in <see cref="WindowOverlay"/>, newest at
/// bottom) with variant icon, body, close cross and at most one action link. Appearance
/// belongs to the shell theme (Themes/Controls/NotificationToast.axaml). View invokes it through
/// <see cref="Show"/>.
/// Information and warning auto-dismiss after a delay defined by variant theme constants
/// (DungeonNotificationInformationDismissDelay, DungeonNotificationWarningDismissDelay) - not
/// an invocation parameter or notification field; pointer over a toast pauses dismissal, and leaving
/// restarts the countdown. Error stays until dismissed. Clicking an action runs it
/// and closes the toast. Notification is a view: does not change campaign state by itself.
/// </summary>
[TemplatePart(CloseButtonPart, typeof(Button))]
[TemplatePart(ActionButtonPart, typeof(Button))]
[PseudoClasses(InformationPseudoClass, WarningPseudoClass, ErrorPseudoClass)]
public sealed class NotificationToast : TemplatedControl
{
    public const string CloseButtonPart = "PART_CloseButton";
    public const string ActionButtonPart = "PART_ActionButton";
    public const string InformationPseudoClass = ":information";
    public const string WarningPseudoClass = ":warning";
    public const string ErrorPseudoClass = ":error";

    public static readonly StyledProperty<NotificationKind> KindProperty =
        AvaloniaProperty.Register<NotificationToast, NotificationKind>(nameof(Kind));

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<NotificationToast, string?>(nameof(Message));

    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<NotificationToast, string?>(nameof(ActionText));

    private Action? action;
    private Button? closeButton;
    private Button? actionButton;
    private IDisposable? pendingDismiss;

    public NotificationToast()
    {
        UpdatePseudoClasses();
    }

    public NotificationKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>Action-link label; no label means no link.</summary>
    public string? ActionText
    {
        get => GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    /// <summary>
    /// Shows a notification in the window containing <paramref name="origin"/>. Action (label
    /// and callback) is optional; link appears only when both are supplied.
    /// </summary>
    public static void Show(Visual origin, NotificationKind kind, string message, string? actionText = null, Action? action = null)
    {
        ArgumentNullException.ThrowIfNull(origin);
        var overlay = WindowOverlay.Find(origin)
            ?? throw new InvalidOperationException("Okno elementu nie ma warstwy WindowOverlay (MainWindow.axaml).");

        var hasAction = !string.IsNullOrEmpty(actionText) && action is not null;
        var toast = new NotificationToast
        {
            Kind = kind,
            Message = message,
            ActionText = hasAction ? actionText : null,
            action = hasAction ? action : null,
        };
        overlay.Toasts.Children.Add(toast);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (closeButton is not null)
        {
            closeButton.Click -= OnCloseClick;
        }

        if (actionButton is not null)
        {
            actionButton.Click -= OnActionClick;
        }

        closeButton = e.NameScope.Find<Button>(CloseButtonPart);
        actionButton = e.NameScope.Find<Button>(ActionButtonPart);

        if (closeButton is not null)
        {
            closeButton.Click += OnCloseClick;
        }

        if (actionButton is not null)
        {
            actionButton.Click += OnActionClick;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == KindProperty)
        {
            UpdatePseudoClasses();
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        var offset = this.TryFindResource("DungeonNotificationOpenOffset", out var o) && o is double value ? value : 12d;
        OverlayAppearMotion.Run(this, this, offset, 0d);
        StartDismissCountdown();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        StopDismissCountdown();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        StartDismissCountdown();
    }

    private void StartDismissCountdown()
    {
        StopDismissCountdown();
        var key = Kind switch
        {
            NotificationKind.Information => "DungeonNotificationInformationDismissDelay",
            NotificationKind.Warning => "DungeonNotificationWarningDismissDelay",
            _ => null,
        };

        if (key is null || !this.TryFindResource(key, out var d) || d is not TimeSpan delay)
        {
            return;
        }

        pendingDismiss = DispatcherTimer.RunOnce(Close, delay);
    }

    private void StopDismissCountdown()
    {
        pendingDismiss?.Dispose();
        pendingDismiss = null;
    }

    private void Close()
    {
        StopDismissCountdown();
        (Parent as Panel)?.Children.Remove(this);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnActionClick(object? sender, RoutedEventArgs e)
    {
        var run = action;
        Close();
        run?.Invoke();
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(InformationPseudoClass, Kind == NotificationKind.Information);
        PseudoClasses.Set(WarningPseudoClass, Kind == NotificationKind.Warning);
        PseudoClasses.Set(ErrorPseudoClass, Kind == NotificationKind.Error);
    }
}
