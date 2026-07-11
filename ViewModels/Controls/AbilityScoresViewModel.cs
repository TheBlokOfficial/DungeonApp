using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DungeonApp.Models.ContentPacks.Components;

namespace DungeonApp.ViewModels.Controls;

/// <summary>
/// Uniwersalny ViewModel do obsługi komponentu AbilityScoresTable.
/// </summary>
/// <remarks>
/// Został wydzielony z głównych modeli widoku (np. AdversariesTabViewModel), 
/// aby umożliwić wielokrotne wykorzystanie tabeli atrybutów w całej aplikacji 
/// bez powielania logiki matematycznej liczącej modyfikatory. W przyszłości można 
/// tu dopiąć obsługę biegłości (Proficiencies).
/// </remarks>
public partial class AbilityScoresViewModel : ObservableObject
{
    public AbilityScores Abilities { get; }

    public AbilityScoresViewModel(AbilityScores abilities)
    {
        Abilities = abilities ?? new AbilityScores();
    }

    private string GetModifierString(int score)
    {
        int mod = (int)Math.Floor((score - 10) / 2.0);
        return mod >= 0 ? $"+{mod}" : $"{mod}";
    }

    public string StrMod => GetModifierString(Abilities.Strength);
    public string DexMod => GetModifierString(Abilities.Dexterity);
    public string ConMod => GetModifierString(Abilities.Constitution);
    public string IntMod => GetModifierString(Abilities.Intelligence);
    public string WisMod => GetModifierString(Abilities.Wisdom);
    public string ChaMod => GetModifierString(Abilities.Charisma);

    public string StrSave => StrMod;
    public string DexSave => DexMod;
    public string ConSave => ConMod;
    public string IntSave => IntMod;
    public string WisSave => WisMod;
    public string ChaSave => ChaMod;
}
