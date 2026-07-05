using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.Models;

public partial class Combatant : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _initiative;

    [ObservableProperty]
    private int _currentHp;

    [ObservableProperty]
    private int _maxHp;

    [ObservableProperty]
    private bool _isEnemy;

    [ObservableProperty]
    private bool _isActiveTurn;

    [ObservableProperty]
    private bool _isDead;
}
