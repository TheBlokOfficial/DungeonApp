using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;

namespace DungeonApp.Controls;

/// <summary>
/// Statyczny panel modułu z możliwością zmiany rozmiaru (resize grip ◢).
/// Przeznaczony wyłącznie do użytku w środowisku Sandbox/Workbench.
/// </summary>
/// <remarks>
/// DLACZEGO drag został usunięty:
/// Panel jest wyśrodkowany przez HorizontalAlignment/VerticalAlignment="Center"
/// w kontenerze widoku. Przeciąganie nie ma sensu w tym układzie — po puszczeniu
/// panel i tak wróciłby na środek. Resize grip pozwala testować responsywność
/// modułu w różnych wymiarach, co jest jedyną potrzebną interakcją.
///
/// DLACZEGO logika resize jest w code-behind, a nie w ViewModelu:
/// Width/Height to czysto wizualna odpowiedzialność widoku — nie ma żadnej
/// logiki domenowej. ViewModel nie powinien wiedzieć o pikselach ekranu.
///
/// DLACZEGO Pointer.Capture:
/// Bez przechwycenia wskaźnika (Capture), gdy użytkownik ciągnie szybko
/// i kursor wyjdzie poza granice elementu, zdarzenia PointerMoved przestają
/// trafiać do handlera — efekt: resize się zrywa.
/// </remarks>
public partial class FloatingPanel : UserControl
{
    // ─── StyledProperty: PanelTitle ───────────────────────────────────────────
    /// <summary>Tytuł wyświetlany na info barze (np. "Core.Timekeeper").</summary>
    public static readonly StyledProperty<string> PanelTitleProperty =
        AvaloniaProperty.Register<FloatingPanel, string>(nameof(PanelTitle), defaultValue: "Moduł");

    public string PanelTitle
    {
        get => GetValue(PanelTitleProperty);
        set => SetValue(PanelTitleProperty, value);
    }

    // ─── StyledProperty: PanelContent ─────────────────────────────────────────
    /// <summary>Treść wstrzykiwana wewnątrz panelu (np. TimekeeperView).</summary>
    public static readonly StyledProperty<object?> PanelContentProperty =
        AvaloniaProperty.Register<FloatingPanel, object?>(nameof(PanelContent));

    public object? PanelContent
    {
        get => GetValue(PanelContentProperty);
        set => SetValue(PanelContentProperty, value);
    }

    // ─── Stałe wymiarów ───────────────────────────────────────────────────────
    public const double DefaultWidth  = 520;
    public const double DefaultHeight = 380;
    public const double MinPanelWidth  = 260;
    public const double MinPanelHeight = 160;

    // ─── Stan zmiany rozmiaru ─────────────────────────────────────────────────
    private bool   _isResizing;
    private Point  _resizeStartPointer;
    private double _resizeStartWidth;
    private double _resizeStartHeight;

    public FloatingPanel()
    {
        InitializeComponent();

        Width  = DefaultWidth;
        Height = DefaultHeight;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PanelTitleProperty)
            UpdateTitleText();

        // DLACZEGO ręczna aktualizacja zamiast bindowania XAML:
        // RelativeSource AncestorType jest składnią WPF i rzuca wyjątek
        // podczas parsowania XAML w Avalonia 12, psując cały widok-rodzic.
        // Ręczne ustawienie przez OnPropertyChanged jest 100% niezawodne.
        if (change.Property == PanelContentProperty)
            UpdateContent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        WireUpResizeInteraction();
        UpdateTitleText();
        UpdateContent();
        UpdateSizeLabel();
    }

    // ─── Podpięcie handlera resize ────────────────────────────────────────────

    private void WireUpResizeInteraction()
    {
        var resizeGrip = this.FindControl<Border>("ResizeGrip");

        if (resizeGrip is not null)
        {
            resizeGrip.PointerPressed  += ResizeGrip_PointerPressed;
            resizeGrip.PointerMoved    += ResizeGrip_PointerMoved;
            resizeGrip.PointerReleased += ResizeGrip_PointerReleased;
        }
    }

    // ─── RESIZE: Zmiana rozmiaru ──────────────────────────────────────────────

    private void ResizeGrip_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        _isResizing = true;
        _resizeStartPointer = e.GetPosition(null); // Koordynaty ekranowe
        _resizeStartWidth   = Width;
        _resizeStartHeight  = Height;

        e.Pointer.Capture(sender as IInputElement);
        e.Handled = true;
    }

    private void ResizeGrip_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isResizing) return;

        var currentPos = e.GetPosition(null);
        var deltaX = currentPos.X - _resizeStartPointer.X;
        var deltaY = currentPos.Y - _resizeStartPointer.Y;

        Width  = Math.Max(MinPanelWidth,  _resizeStartWidth  + deltaX);
        Height = Math.Max(MinPanelHeight, _resizeStartHeight + deltaY);

        UpdateSizeLabel();
        e.Handled = true;
    }

    public event EventHandler? ResizeCompleted;

    private void ResizeGrip_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isResizing = false;
        e.Pointer.Capture(null);
        e.Handled = true;
        ResizeCompleted?.Invoke(this, EventArgs.Empty);
    }

    // ─── Pomocniki UI ─────────────────────────────────────────────────────────

    private void UpdateTitleText()
    {
        var titleText = this.FindControl<TextBlock>("TitleText");
        if (titleText is not null)
            titleText.Text = PanelTitle;
    }

    /// <summary>
    /// Ustawia Content ContentPresenter na aktualną wartość PanelContent.
    /// Wywoływana przy OnLoaded i przy każdej zmianie PanelContent.
    /// </summary>
    private void UpdateContent()
    {
        var contentArea = this.FindControl<ContentPresenter>("ContentArea");
        if (contentArea is not null)
            contentArea.Content = PanelContent;
    }

    /// <summary>
    /// Aktualizuje etykietę wymiarów w info barze (np. "520 × 380").
    /// Przydatne przy ręcznym testowaniu responsywności modułów.
    /// </summary>
    private void UpdateSizeLabel()
    {
        var sizeLabel = this.FindControl<TextBlock>("SizeLabel");
        if (sizeLabel is not null)
            sizeLabel.Text = $"{(int)Width} × {(int)Height}";
    }
}
