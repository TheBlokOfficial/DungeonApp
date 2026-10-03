namespace DungeonApp.Desktop.Entries.Controls;

/// <summary>
/// One named paragraph a <see cref="ProseSectionView"/> shows: its name, an optional short note the
/// card shows muted after the name ("3 na dzień"), and its text, which may carry <c>**…**</c>
/// emphasis (<see cref="DungeonApp.Desktop.Controls.EmphasisMarkup"/>). The card view builds these
/// from its own record; the structure was written by the pack's author, never read out of the text.
/// </summary>
public sealed record ProseItem(string Name, string? Note, string Text)
{
    /// <summary>
    /// The note in brackets, starting with the space that parts it from the name - both run on in one
    /// wrapping line, and a copied selection reads "Leczący dotyk (3 na dzień)" the way a statblock
    /// prints it.
    /// </summary>
    public string? NoteDisplay => Note is null ? null : $" ({Note})";
}
