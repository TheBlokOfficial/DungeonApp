using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.Models;

public partial class PlayerCharacter : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _characterName = string.Empty;

    [ObservableProperty]
    private string _playerName = string.Empty; // Żebyś wiedział, kto gra tym modelem

    [ObservableProperty]
    private string _race = string.Empty;

    [ObservableProperty]
    private string _classAndLevel = string.Empty;

    // Miejsce na przyszłe statystyki (HP, AC, Szybkość, Ekwipunek)
}