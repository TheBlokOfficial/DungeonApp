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