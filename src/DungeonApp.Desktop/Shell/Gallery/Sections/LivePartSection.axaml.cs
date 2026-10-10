using Avalonia.Controls;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

/// <summary>Sample values for the live part's controls.</summary>
public partial class LivePartSection : UserControl
{
    public LivePartSection()
    {
        InitializeComponent();
        FillNoteSamples();
        FillBaseSamples();
    }

    private void FillNoteSamples()
    {
        NoteOneLine.Text = "Przekupiony przez gildię złodziei.";
        NoteLines.Text = "Ukradł klucz do skarbca — trzyma go w lewej sakwie.\nUcieknie, gdy zostanie sam.";
        NoteDisabled.Text = "Nie wie, że drużyna go śledzi.";

        // The working sample shows what its keeper would receive on leaving the field.
        NoteLines.Committed += (_, e) => NoteCommitted.Text = $"Zatwierdzono notatkę, znaków: {e.Text.Length}.";
    }

    // Written out, not computed: the frame has no arithmetic of any system. The lowered score keeps
    // a positive modifier, so the cell tint and the digit colour are seen to mean different things.
    private void FillBaseSamples()
    {
        BaseLeftTable.Rows =
        [
            new AbilityRow("SIŁ", "8", "−1", ValueTone.Negative),
            new AbilityRow("ZRĘ", "12", "+1", ValueTone.Positive, BaseDeviation.Below),
            new AbilityRow("KON", "10", "+0"),
        ];
        BaseRightTable.Rows =
        [
            new AbilityRow("INT", "10", "+0"),
            new AbilityRow("MDR", "8", "−1", ValueTone.Negative),
            new AbilityRow("CHA", "18", "+4", ValueTone.Positive, BaseDeviation.Above),
        ];
    }
}
