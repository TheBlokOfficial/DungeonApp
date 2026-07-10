using System.Collections.Generic;

namespace DungeonApp.Models.ContentPacks.Components;

public class AbilityScores
{
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public int Charisma { get; set; } = 10;
}

public class CombatStats
{
    public int ArmorClass { get; set; } = 10;
    public string ArmorType { get; set; } = string.Empty;
    public int MaxHp { get; set; } = 10;
    public string HitDice { get; set; } = string.Empty;
    public int InitiativeBonus { get; set; } = 0;
    public int Xp { get; set; } = 0;
}

public class SpeedComponent
{
    public string Type { get; set; } = "Walk";
    public int Value { get; set; } = 30;
    public string? Note { get; set; }
}

public class ActionDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
