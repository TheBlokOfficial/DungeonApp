using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace DungeonApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.AddHandler(PointerPressedEvent, OnGlobalPointerPressed, RoutingStrategies.Tunnel);
        this.SizeChanged += OnSizeChanged;
        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (this.DataContext is ViewModels.MainWindowViewModel vm)
        {
            vm.PropertyChanged -= Vm_PropertyChanged;
            vm.PropertyChanged += Vm_PropertyChanged;
            UpdateEffectiveScale();
        }
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.MainWindowViewModel.UiScale))
        {
            UpdateEffectiveScale();
        }
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateEffectiveScale();
    }

    private const double ComfortZoneWidth = 1440;
    private const double ComfortZoneHeight = 720;

    /// <summary>
    /// Główny silnik zarządzający skalowaniem całego interfejsu aplikacji ("Safe-Shrink").
    /// Metoda ta wylicza finalny mnożnik rozmiaru dla ViewBoxa.
    /// </summary>
    /// <remarks>
    /// **Dlaczego to robimy w ten sposób:**
    /// 1. **DPI Compensation:** Avalonia domyślnie skaluje interfejs w oparciu o ustawienia wyświetlacza Windows (np. 150%). 
    ///    Aby temu zapobiec i zachować absolutną kontrolę nad wyglądem UI (pixel-perfect na 1440p), dzielimy docelową 
    ///    skalę przez <c>renderScaling</c> (DPI systemu). Skala ustawiana przez gracza jest fizycznym rozmiarem końcowym na ekranie.
    /// 2. **Auto-Scaling (0.0):** Jeśli ustawiono Auto, aplikacja bazuje na fizycznych pikselach monitora (np. przy 1440p 
    ///    zawsze dobierze skalę 1.0, niezależnie czy w Windowsie jest ustawione 100% czy 150%).
    /// 3. **Safe-Shrink:** Obliczamy <c>availableScale</c> bazując na rozmiarach okna i <c>ComfortZone</c> (1440x720). 
    ///    Jeśli użytkownik zmniejszy okno poniżej tego rozmiaru, aplikacja płynnie pomniejszy UI zapobiegając ucinaniu kontrolek,
    ///    utrzymując układ 16:9 w strefie komfortu.
    /// </remarks>
    private void UpdateEffectiveScale()
    {
        if (this.DataContext is ViewModels.MainWindowViewModel vm)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            double renderScaling = topLevel?.RenderScaling ?? 1.0;
            double physicalWidth = this.Bounds.Width * renderScaling;
            
            double maxScale;

            if (vm.UiScale == 0.0) // Auto — determine MaxScale from monitor resolution
            {
                double desiredScale;
                if (physicalWidth >= 3000) desiredScale = 1.5;        // 4K
                else if (physicalWidth >= 2000) desiredScale = 1.0;   // 1440p (sweet spot)
                else if (physicalWidth >= 1300) desiredScale = 0.75;  // 1080p
                else desiredScale = 0.5;                              // 720p / small windows
                
                maxScale = desiredScale / renderScaling;
            }
            else
            {
                maxScale = vm.UiScale / renderScaling; // User-chosen maximum physical scale
            }

            // Safe-Shrink: clamp scale down if window is too small for the comfort zone
            double availableScaleW = this.Bounds.Width / ComfortZoneWidth;
            double availableScaleH = this.Bounds.Height / ComfortZoneHeight;
            double availableScale = Math.Min(availableScaleW, availableScaleH);

            vm.EffectiveUiScale = Math.Min(maxScale, availableScale);
        }
    }

    private void OnGlobalPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var visual = e.Source as Avalonia.Visual;
        bool isTextBox = visual is TextBox || visual?.FindAncestorOfType<TextBox>() != null;
        
        var button = visual as Button ?? visual?.FindAncestorOfType<Button>();
        bool isUnfocusableButton = button != null && !button.Focusable;
        
        if (!isTextBox && !isUnfocusableButton)
        {
            this.Focus();
        }
    }
}
