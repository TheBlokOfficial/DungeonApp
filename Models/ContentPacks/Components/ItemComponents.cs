using System.Collections.Generic;

namespace DungeonApp.Models.ContentPacks.Components;

public class ItemComponents
{
    public Weapon? Weapon { get; set; }
    public Armor? Armor { get; set; }
    public Consumable? Consumable { get; set; }
    public List<ModifierDefinition> Modifiers { get; set; } = new();
}

public class Weapon
{
    public string DamageRoll { get; set; } = string.Empty;
    public string DamageType { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;
    public List<string> WeaponProperties { get; set; } = new();
}

public class Armor
{
    public int ArmorBonus { get; set; }
    public string ArmorType { get; set; } = string.Empty;
    public bool StealthDisadvantage { get; set; }
    public int StrengthRequirement { get; set; }
    public int? MaxDexterityBonus { get; set; }
}

public class Consumable
{
    public int Uses { get; set; }
}

public class ModifierDefinition
{
    public string Target { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
}
