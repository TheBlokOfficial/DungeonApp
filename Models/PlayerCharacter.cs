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

    [ObservableProperty]
    private int _maxHp = 10;

    [ObservableProperty]
    private int _currentHp = 10;

    [ObservableProperty]
    private int _armorClass = 10;

    [ObservableProperty]
    private int _speed = 30;

    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<EquipmentItem> _equipmentItems = new();

    [ObservableProperty]
    private string _notes = string.Empty;
}