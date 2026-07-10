using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DungeonApp.Views.Dashboard;

public partial class AdversariesTabView : UserControl
{
    public AdversariesTabView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
