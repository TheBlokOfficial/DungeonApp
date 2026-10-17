using System;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Entries.Controls;

/// <summary>
/// The frame's sections of an entity's live part (<see cref="EntityLiveContext.CreateSections"/>):
/// the GM's note. The note is saved when the GM leaves the field, and when the window lets the card
/// go (<see cref="EntityLiveContext.CommitRequested"/>) - never per keystroke, never after a delay.
/// A change made elsewhere (the same entity in another window) shows here unless the GM is typing.
/// </summary>
public partial class EntityLiveSectionsView : UserControl
{
    public const string NotePlaceholder = "Notatka MG — kliknij, żeby pisać";

    private readonly EntityLiveContext? _live;

    // What was last handed to the campaign: a commit on leaving the field and the window's commit
    // right after it would otherwise send the same note twice, the second refused while the first saves.
    private string? _committed;

    /// <summary>For the XAML loader and the designer; a working section is built with a context.</summary>
    public EntityLiveSectionsView()
    {
        InitializeComponent();
    }

    public EntityLiveSectionsView(EntityLiveContext live)
        : this()
    {
        ArgumentNullException.ThrowIfNull(live);

        _live = live;
        _committed = live.Part.Note;
        NoteField.Text = live.Part.Note;
        NoteField.Committed += (_, e) => Commit(e.Text);
        live.Changed += OnLiveChanged;
        live.CommitRequested += () => Commit(NoteField.Text);
    }

    private void OnLiveChanged()
    {
        var note = _live!.Part.Note;
        _committed = note;
        if (!NoteField.IsFocused)
        {
            NoteField.Text = note;
        }
    }

    // async void: the commit is the end of a GM action, so its failure goes to the UI thread's handler.
    private async void Commit(string? text)
    {
        if (_live is null || EntityLiveContext.SameNote(text, _committed))
        {
            return;
        }

        _committed = text;
        await _live.SetNoteAsync(text);
    }
}
