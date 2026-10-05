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
        CheckListSample.ItemsSource = options;
        CheckListSample.SelectedItems?.Add(options[1]);

        string[] rarities = ["Pospolity", "Niepospolity", "Rzadki", "Bardzo rzadki", "Legendarny"];
        string[] colors = ["Czerwony", "Zielony", "Niebieski"];
        string[] sizes = ["Mały", "Średni", "Duży"];
        ChipPickerNone.ItemsSource = rarities;
        ChipPickerOne.ItemsSource = rarities;
        ChipPickerOne.SelectedItems = new AvaloniaList<object> { "Rzadki" };
        ChipPickerSeveral.ItemsSource = rarities;
        // Selected in a different order from the list - the label uses the first in list order.
        ChipPickerSeveral.SelectedItems = new AvaloniaList<object> { "Legendarny", "Rzadki", "Bardzo rzadki" };
        ChipPickerDisabled.ItemsSource = colors;
        ChipPickerDisabledSelected.ItemsSource = colors;
        ChipPickerDisabledSelected.SelectedItems = new AvaloniaList<object> { "Zielony" };
        ChipPickerLong.ItemsSource = new[] { "Bardzo długa wartość filtra, która nie mieści się w chipie", "Krótka" };
        ChipPickerLong.SelectedItems = new AvaloniaList<object> { "Bardzo długa wartość filtra, która nie mieści się w chipie" };
        ChipSingleNone.ItemsSource = sizes;
        ChipSingleOne.ItemsSource = sizes;
        ChipSingleOne.SelectedItem = "Duży";
        FilterType.ItemsSource = LongNames;
        FilterType.SelectedItems = new AvaloniaList<object> { "Goblin", "Ork" };
        FilterSize.ItemsSource = new[] { "Mały", "Średni", "Duży" };
        FilterSource.ItemsSource = new GalleryOption[] { new("Podręcznik"), new("Dodatek"), new("Własne") };
        FilterValues.ItemsSource = chipValues;
        ObjectPicker.ItemsSource = options;
        ObjectPicker.SelectedItems = new AvaloniaList<object> { options[1] };
    }
}
