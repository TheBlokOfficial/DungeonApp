using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DungeonApp.Views.Dashboard;

public partial class ItemsTabView : UserControl
{
    public ItemsTabView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
