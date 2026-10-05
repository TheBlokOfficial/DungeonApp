using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Confirmation dialog: question over the whole application window - scrim (DungeonScrimBrush) over the rest
/// of the window, centred card with title, body, "Anuluj" and action buttons. Appearance belongs to the shell
/// theme (Themes/Controls/ConfirmationDialog.axaml). The view invokes it through <see cref="ShowAsync"/>.
/// Behaviour: Escape = "Anuluj"; clicking the scrim does not close; Enter confirms only a primary action,
/// doing nothing for destructive actions. Scrim intercepts the pointer above underlying content, Tab cycles within the card;
/// closing restores focus to its previous location. Dialog is a view: returns an answer, caller
/// changes state.
/// </summary>
[TemplatePart(CancelButtonPart, typeof(Button))]
[TemplatePart(ConfirmButtonPart, typeof(Button))]
[TemplatePart(CardPart, typeof(Control))]
public sealed class ConfirmationDialog : TemplatedControl
{
    public const string CancelButtonPart = "PART_CancelButton";
    public const string ConfirmButtonPart = "PART_ConfirmButton";
    public const string CardPart = "PART_Card";

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ConfirmationDialog, string?>(nameof(Title));

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<ConfirmationDialog, string?>(nameof(Message));

    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<ConfirmationDialog, string?>(nameof(ActionText));

    public static readonly StyledProperty<bool> IsDestructiveProperty =
        AvaloniaProperty.Register<ConfirmationDialog, bool>(nameof(IsDestructive));

    private readonly TaskCompletionSource<bool> answer = new();
    private Button? cancelButton;
    private Button? confirmButton;
    private Control? card;
    private IInputElement? returnFocusTo;
    private WindowOverlay? host;

    public ConfirmationDialog()
    {
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string? ActionText
    {
        get => GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    /// <summary>Destructive action: .danger action button, Enter does nothing.</summary>
    public bool IsDestructive
    {
        get => GetValue(IsDestructiveProperty);
        set => SetValue(IsDestructiveProperty, value);
    }

    /// <summary>
    /// Shows a question over the window containing <paramref name="origin"/>; completes with true when
    /// confirmed, false when cancelled.
    /// </summary>
    public static Task<bool> ShowAsync(Visual origin, string title, string message, string actionText, bool isDestructive)
    {
        ArgumentNullException.ThrowIfNull(origin);
        var overlay = WindowOverlay.Find(origin)
            ?? throw new InvalidOperationException("Okno elementu nie ma warstwy WindowOverlay (MainWindow.axaml).");

        var dialog = new ConfirmationDialog
        {
            Title = title,
            Message = message,
            ActionText = actionText,
            IsDestructive = isDestructive,
        };
        dialog.Open(overlay, TopLevel.GetTopLevel(origin)?.FocusManager?.GetFocusedElement() ?? origin as IInputElement);
        return dialog.answer.Task;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (cancelButton is not null)
        {
            cancelButton.Click -= OnCancelClick;
        }

        if (confirmButton is not null)
        {
            confirmButton.Click -= OnConfirmClick;
        }

        cancelButton = e.NameScope.Find<Button>(CancelButtonPart);
        confirmButton = e.NameScope.Find<Button>(ConfirmButtonPart);
        card = e.NameScope.Find<Control>(CardPart);

        if (cancelButton is not null)
        {
            cancelButton.Click += OnCancelClick;
        }

        if (confirmButton is not null)
        {
            confirmButton.Click += OnConfirmClick;
        }

        UpdateActionVariant();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsDestructiveProperty)
        {
            UpdateActionVariant();
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        cancelButton?.Focus(NavigationMethod.Tab);

        var offset = this.TryFindResource("DungeonPopupOpenOffset", out var o) && o is double value ? value : 4d;
        OverlayAppearMotion.Run(this, card ?? this, 0d, offset);
    }

    // Scrim consumes the click - neither closes the dialog nor passes it through.
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        e.Handled = true;
    }

    private void Open(WindowOverlay overlay, IInputElement? previousFocus)
    {
        host = overlay;
        returnFocusTo = previousFocus;
        overlay.Children.Add(this);
    }

    private void Close(bool confirmed)
    {
        if (host is null)
        {
            return;
        }

        host.Children.Remove(this);
        host = null;

        if (returnFocusTo is Visual visual && visual.IsAttachedToVisualTree())
        {
            returnFocusTo.Focus();
        }

        answer.TrySetResult(confirmed);
    }

    private void UpdateActionVariant()
    {
        if (confirmButton is null)
        {
            return;
        }

        confirmButton.Classes.Set("primary", !IsDestructive);
        confirmButton.Classes.Set("danger", IsDestructive);
    }

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                e.Handled = true;
                Close(confirmed: false);
                break;
            case Key.Enter:
                // Enter does nothing for destructive actions - even on a focused button.
                e.Handled = true;
                if (!IsDestructive)
                {
                    Close(confirmed: true);
                }

                break;
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(confirmed: false);

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Close(confirmed: true);
}
