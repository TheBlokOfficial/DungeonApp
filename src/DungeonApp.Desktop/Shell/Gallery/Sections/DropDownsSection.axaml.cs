using Avalonia.Controls;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

public partial class DropDownsSection : UserControl
{
    // About thirty names - longer than the flyout's and the drop-down's highest height.
    private static readonly string[] Monsters =
    [
        "Aboleth", "Bazyliszek", "Behir", "Bugbear", "Chimera", "Cyklop", "Drider", "Driada",
        "Ettin", "Gargulec", "Ghul", "Gnoll", "Goblin", "Gorgona", "Harpia", "Hobgoblin",
        "Hydra", "Kobold", "Krakon", "Lisz", "Mantykora", "Meduza", "Minotaur", "Mumia",
        "Ogr", "Ork", "Pająk olbrzymi", "Smok czerwony", "Szkielet", "Troll", "Upiór", "Żywiołak ziemi",
    ];

    public DropDownsSection()
    {
        InitializeComponent();
        LongFlyoutList.ItemsSource = Monsters;
    }
}
