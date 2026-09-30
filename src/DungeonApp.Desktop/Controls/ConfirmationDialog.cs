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
/// Okno potwierdzenia: pytanie nad całym oknem aplikacji - zasłona (DungeonScrimBrush) na reszcie
/// okna, karta na środku z tytułem, treścią i przyciskami "Anuluj" oraz akcji. Wygląd należy do motywu
/// ramy (Themes/Controls/ConfirmationDialog.axaml). Widok wywołuje je jedną metodą <see cref="ShowAsync"/>.
/// Zachowanie: Escape = Anuluj; kliknięcie w zasłonę nie zamyka; Enter potwierdza tylko akcję główną,
/// przy niszczącej nic nie robi. Zasłona zabiera mysz treści pod spodem, Tab krąży po karcie; po
/// zamknięciu fokus wraca tam, gdzie był przed otwarciem. Okno jest widokiem: zwraca odpowiedź, stan
/// zmienia wywołujący.
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

    /// <summary>Akcja niszcząca: przycisk akcji w odmianie .danger, Enter nic nie robi.</summary>
    public bool IsDestructive
    {
        get => GetValue(IsDestructiveProperty);
        set => SetValue(IsDestructiveProperty, value);
    }

    /// <summary>
    /// Pokazuje pytanie nad oknem, w którym stoi <paramref name="origin"/>; kończy się true, gdy
    /// potwierdzono, false, gdy anulowano.
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

    // Zasłona zabiera kliknięcie - nie zamyka okna i nie przepuszcza go dalej.
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
                // Przy akcji niszczącej Enter nic nie robi - także na przycisku z fokusem.
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
