using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

public partial class ButtonsSection : UserControl
{
    // Przykładowe pola sortowania: jedno tekstowe (napisy A–Z / Z–A) i dwa liczbowe (rosnąco /
    // malejąco). Neutralne - rama nie nazywa rodzajów wpisu.
    private static readonly string[] SortOptions = ["Nazwa", "Waga", "Cena"];
    private static readonly string[] NumericSortOptions = ["Waga", "Cena"];

    public ButtonsSection()
    {
        InitializeComponent();

        foreach (var picker in new[] { SortSample, SortSampleFixedDirection, SortSampleDisabled })
        {
            picker.Options = SortOptions;
            picker.NumericOptions = NumericSortOptions;
            picker.SelectedOption = SortOptions[0];
        }

        SortSampleDisabled.SelectedOption = SortOptions[1];

        // Komenda, której nie da się wykonać - przycisk wyłącza ona, nie IsEnabled.
        DisabledByCommandSample.Command = new RelayCommand(() => { }, () => false);
    }
}
