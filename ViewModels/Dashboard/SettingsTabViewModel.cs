using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DungeonApp.Services;

namespace DungeonApp.ViewModels.Dashboard;

public partial class SettingsTabViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    public record ScaleOption(string DisplayName, double Value);

    public ObservableCollection<ScaleOption> SettingsScaleOptions { get; } =
    [
        new("Auto", 0.0),
        new("Mały (75%)", 0.75),
        new("Średni (100%)", 1.0),
        new("Duży (125%)", 1.25),
        new("Ogromny (150%)", 1.50)
    ];

    [ObservableProperty]
    private double _uiScale;

    private ScaleOption _selectedScaleOption;
    public ScaleOption SelectedScaleOption
    {
        get => _selectedScaleOption;
        set
        {
            if (SetProperty(ref _selectedScaleOption, value))
            {
                UiScale = value.Value;
                
                var currentSettings = _settingsService.LoadSettings();
                currentSettings.UiScale = value.Value;
                _settingsService.SaveSettings(currentSettings);
            }
        }
    }

    public SettingsTabViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        
        var settings = _settingsService.LoadSettings();
        UiScale = settings.UiScale;
        _selectedScaleOption = SettingsScaleOptions.FirstOrDefault(x => Math.Abs(x.Value - UiScale) < 0.01) ?? SettingsScaleOptions[2];
    }
}
