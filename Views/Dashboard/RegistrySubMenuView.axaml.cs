using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DungeonApp.Views.Dashboard;

public partial class RegistrySubMenuView : UserControl
{
    public RegistrySubMenuView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
