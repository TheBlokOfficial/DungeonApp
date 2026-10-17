using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Desktop.Entries;

/// <summary>
/// What the frame gives a card that stands for an entity: the entity's live part as it is now
/// (<see cref="Part"/>), the GM's changes to it, which go through the campaign's one door and are
/// saved at once, and a notice when the part was read again (<see cref="Changed"/>). A card in the
/// library gets none and looks as it always does.
/// <para>
/// A change in the live part never rebuilds the card: the window that owns the context reads the
/// entity again after every campaign change (<see cref="Refresh"/>), and the card only updates the
/// controls it bound, so the scroll position and the keyboard focus stay where they were.
/// </para>
/// </summary>
public sealed class EntityLiveContext
{
    private readonly IEntityValueCatalog _values;
    private readonly Func<CampaignChange, Task<CampaignChangeResult>> _change;

    public EntityLiveContext(
        ResolvedEntity resolved, IEntityValueCatalog values, Func<CampaignChange, Task<CampaignChangeResult>> change)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(change);

        _values = values;
        _change = change;
        Part = EntityLivePart.Read(resolved, values);
    }

    /// <summary>The live part as of the last read.</summary>
    public EntityLivePart Part { get; private set; }

    /// <summary>Raised after <see cref="Part"/> was read again because the entity changed.</summary>
    public event Action? Changed;

    /// <summary>
    /// Raised when the window lets the card go (another entity, a closed window): an edit still open
    /// in a field is saved now, because its field may never get to lose the focus.
    /// </summary>
    public event Action? CommitRequested;

    /// <summary>Reads the live part again from <paramref name="resolved"/> when the entity changed.</summary>
    public void Refresh(ResolvedEntity resolved)
    {
        ArgumentNullException.ThrowIfNull(resolved);

        if (resolved.Entity.Equals(Part.Entity))
        {
            return;
        }

        Part = EntityLivePart.Read(resolved, _values);
        Changed?.Invoke();
    }

    /// <summary>Applies <paramref name="change"/> to the tracked value <paramref name="key"/> by the system's arithmetic.</summary>
    public Task ChangeAsync(string key, NumberChange change) => _change(Part.Change(key, change));

    /// <summary>Sets the GM's note; text equal to the note as it stands changes nothing.</summary>
    public Task SetNoteAsync(string? note) =>
        SameNote(note, Part.Note) ? Task.CompletedTask : _change(Part.SetNote(note));

    /// <summary>Asks every open field of the card to save what it holds; see <see cref="CommitRequested"/>.</summary>
    public void CommitPending() => CommitRequested?.Invoke();

    /// <summary>
    /// The frame's sections of the live part (the note), which the card places where its layout puts
    /// them, above the line that opens the entry's fixed part.
    /// </summary>
    public Control CreateSections() => new EntityLiveSectionsView(this);

    /// <summary>Blank text is no note, as <see cref="EntityLivePart.SetNote"/> stores it.</summary>
    public static bool SameNote(string? a, string? b) =>
        string.IsNullOrWhiteSpace(a) ? string.IsNullOrWhiteSpace(b) : a == b;
}
