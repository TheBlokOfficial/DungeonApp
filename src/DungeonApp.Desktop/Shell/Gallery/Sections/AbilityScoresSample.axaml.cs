using Avalonia.Controls;
using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

/// <summary>
/// Sample abilities for the gallery's table: every tint shows - positive, negative and zero. The
/// modifiers are written out here, not computed: the frame has no arithmetic of any system.
/// </summary>
public partial class AbilityScoresSample : UserControl
{
    public AbilityScoresSample()
    {
        InitializeComponent();

        LeftTable.Rows =
        [
            new AbilityRow("SIŁ", "8", "−1", ValueTone.Negative),
            new AbilityRow("ZRĘ", "14", "+2", ValueTone.Positive),
            new AbilityRow("KON", "10", "+0"),
        ];
        RightTable.Rows =
        [
            new AbilityRow("INT", "10", "+0"),
            new AbilityRow("MDR", "8", "−1", ValueTone.Negative),
            new AbilityRow("CHA", "18", "+4", ValueTone.Positive),
        ];
    }
}
