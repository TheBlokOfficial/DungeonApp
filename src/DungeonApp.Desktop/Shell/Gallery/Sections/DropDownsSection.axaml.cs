using Avalonia.Collections;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

/// <summary>A gallery sample item that is an object with a name, not a string.</summary>
public sealed class GalleryOption(string name)
{
    public string Name { get; } = name;
}

public partial class DropDownsSection : UserControl
{
    // About thirty names - longer than the flyout's and the drop-down's highest height.
    private static readonly string[] LongNames =
    [
        "Aboleth", "Bazyliszek", "Behir", "Bugbear", "Chimera", "Cyklop", "Drider", "Driada",
        "Ettin", "Gargulec", "Ghul", "Gnoll", "Goblin", "Gorgona", "Harpia", "Hobgoblin",
        "Hydra", "Kobold", "Krakon", "Lisz", "Mantykora", "Meduza", "Minotaur", "Mumia",
        "Ogr", "Ork", "Pająk olbrzymi", "Smok czerwony", "Szkielet", "Troll", "Upiór", "Żywiołak ziemi",
    ];

    public DropDownsSection()
    {
        InitializeComponent();
        LongFlyoutList.ItemsSource = LongNames;
        LongComboBox.ItemsSource = LongNames;

        string[] types = ["Aberracja", "Bestia", "Humanoid", "Nieumarły", "Smok"];
        MultiNone.ItemsSource = types;
        MultiOne.ItemsSource = types;
        MultiOne.SelectedItems = new AvaloniaList<object> { "Humanoid" };
        MultiSeveral.ItemsSource = types;
        MultiSeveral.SelectedItems = new AvaloniaList<object> { "Humanoid", "Nieumarły", "Smok" };
        MultiDisabled.ItemsSource = types;
        MultiDisabled.SelectedItems = new AvaloniaList<object> { "Bestia" };
        MultiLong.ItemsSource = LongNames;
        SearchSingle.ItemsSource = LongNames;
        SearchMultiple.ItemsSource = LongNames;

        string[] chipValues = ["Pierwsza", "Druga", "Trzecia", "Czwarta"];
        ChipRestList.ItemsSource = chipValues;
        ChipActiveList.ItemsSource = chipValues;
        ChipLongList.ItemsSource = LongNames;
        GalleryOption[] options = [new("Pierwsza"), new("Druga"), new("Trzecia"), new("Czwarta")];
        ChipCheckList.ItemsSource = options;
        ObjectPicker.ItemsSource = options;
        ObjectPicker.SelectedItems = new AvaloniaList<object> { options[1] };
    }
}
