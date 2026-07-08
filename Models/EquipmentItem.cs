using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.Models;

public partial class EquipmentItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _quantity = 1;
}
