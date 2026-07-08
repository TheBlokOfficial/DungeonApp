using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DungeonApp.Views.Dashboard;

public partial class OthersTabView : UserControl
{
    public OthersTabView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
