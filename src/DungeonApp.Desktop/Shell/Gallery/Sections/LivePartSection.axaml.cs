using Avalonia.Controls;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

/// <summary>Sample values for the live part's controls.</summary>
public partial class LivePartSection : UserControl
{
    public LivePartSection()
    {
        InitializeComponent();
        FillNoteSamples();
    }

    private void FillNoteSamples()
    {
        NoteOneLine.Text = "Przekupiony przez gildię złodziei.";
        NoteLines.Text = "Ukradł klucz do skarbca — trzyma go w lewej sakwie.\nUcieknie, gdy zostanie sam.";
        NoteDisabled.Text = "Nie wie, że drużyna go śledzi.";

        // The working sample shows what its keeper would receive on leaving the field.
        NoteLines.Committed += (_, e) => NoteCommitted.Text = $"Zatwierdzono notatkę, znaków: {e.Text.Length}.";
    }
}
