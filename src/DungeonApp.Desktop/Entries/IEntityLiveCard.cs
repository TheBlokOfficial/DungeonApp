namespace DungeonApp.Desktop.Entries;

/// <summary>
/// A card that can stand for an entity: given the entity's live part, it shows the entity's numbers
/// in the places of the entry's, lets the GM change what is changed at the table, and places the
/// frame's sections (<see cref="EntityLiveContext.CreateSections"/>) in its layout. Called once,
/// right after the card was created and before its header pieces are read; never in the library.
/// </summary>
public interface IEntityLiveCard
{
    void ShowLive(EntityLiveContext live);
}
