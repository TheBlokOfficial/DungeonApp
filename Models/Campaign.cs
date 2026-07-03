using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.Models;

public partial class Campaign : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _system = "D&D 5e";

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private DateTime _lastSession;

    [ObservableProperty]
    private int _sessionsCount;

    [ObservableProperty]
    private string _description = string.Empty;
}