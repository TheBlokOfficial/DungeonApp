using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.ViewModels.Dashboard;

public partial class FilterItemViewModel : ObservableObject
{
    public string Name { get; }
    public string Value { get; } // ID paczki, tagu itp.
    
    [ObservableProperty]
    private bool _isSelected;

    public FilterItemViewModel(string name, string value, bool isSelected = false)
    {
        Name = name;
        Value = value;
        IsSelected = isSelected;
    }
}

public partial class NumericFilterViewModel : ObservableObject
{
    public string PropertyKey { get; }
    public string DisplayName { get; }
    
    [ObservableProperty]
    private double _minValue;
    
    [ObservableProperty]
    private double _maxValue;
    
    [ObservableProperty]
    private double _currentMin;
    
    [ObservableProperty]
    private double _currentMax;

    public NumericFilterViewModel(string propertyKey, string displayName, double min, double max)
    {
        PropertyKey = propertyKey;
        DisplayName = displayName;
        MinValue = min;
        MaxValue = max;
        CurrentMin = min;
        CurrentMax = max;
    }
}
