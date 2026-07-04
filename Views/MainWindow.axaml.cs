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

    private void UpdateEffectiveScale()
    {
        if (this.DataContext is ViewModels.MainWindowViewModel vm)
        {
            if (vm.UiScale == 0.0) // Auto
            {
                double width = this.Bounds.Width;
                if (width < 1200) vm.EffectiveUiScale = 0.75;
                else if (width < 1600) vm.EffectiveUiScale = 1.0;
                else if (width < 2400) vm.EffectiveUiScale = 1.25;
                else vm.EffectiveUiScale = 1.5;
            }
            else
            {
                vm.EffectiveUiScale = vm.UiScale;
            }
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