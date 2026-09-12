namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// A D&amp;D 5e monster's values. This project is the only place in the application allowed to know
/// what a monster is; the shape below is a designed record, not something assembled from data.
/// </summary>
public sealed record Monster(
    string Size,
    string Type,
    string Alignment,
    int Ac,
    string? AcSource,
    int Hp,
    string? HpDice,
    string Speed,
    int Str,
    int Dex,
    int Con,
    int Int,
    int Wis,
    int Cha,
    string? Skills,
    string Senses,
    string? Languages,
    string Challenge,
    string? SpecialAbilities,
    string Actions,
    string? Description);
