using Avalonia.Controls;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

public partial class TabsSection : UserControl
{
    public TabsSection()
    {
        InitializeComponent();

        string[] views = ["Lista", "Karty", "Tabela"];
        PlainSegments.ItemsSource = views;
        PlainSegments.SelectedIndex = 0;
        UnselectedSegments.ItemsSource = views;
        DisabledSegments.ItemsSource = views;
        DisabledSegments.SelectedIndex = 1;
        StretchedSegments.ItemsSource = views;
        StretchedSegments.SelectedIndex = 2;
        LongSegments.ItemsSource = new[] { "Wszystkie", "Tylko z kampanii i jej obszarów" };
        LongSegments.SelectedIndex = 0;
    }
}
