using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

public partial class ButtonsSection : UserControl
{
    // Sample sort fields: one textual (labels A–Z / Z–A) and two numeric ("rosnąco" /
    // "malejąco"). Neutral - the frame does not name entry types.
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

        // A command that cannot execute - it disables the button, rather than IsEnabled.
        DisabledByCommandSample.Command = new RelayCommand(() => { }, () => false);
    }
}
