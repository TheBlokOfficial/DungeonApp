using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DungeonApp.Views.Controls;

public partial class AbilityScoresTable : UserControl
{
    public AbilityScoresTable()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
