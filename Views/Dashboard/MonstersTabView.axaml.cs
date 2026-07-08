using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DungeonApp.Views.Dashboard;

public partial class MonstersTabView : UserControl
{
    public MonstersTabView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
