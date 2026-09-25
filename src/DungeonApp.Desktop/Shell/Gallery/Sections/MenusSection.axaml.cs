using Avalonia.Controls;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

public partial class MenusSection : UserControl
{
    public MenusSection()
    {
        InitializeComponent();

        string[] filters = ["Wszystkie", "Tylko z kampanii"];
        NarrowSegments.ItemsSource = filters;
        NarrowSegments.SelectedIndex = 0;
        WideSegments.ItemsSource = filters;
        WideSegments.SelectedIndex = 0;
    }
}
