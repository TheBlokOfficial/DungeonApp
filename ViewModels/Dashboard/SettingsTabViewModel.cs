using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DungeonApp.Services;

namespace DungeonApp.ViewModels.Dashboard;

public partial class SettingsTabViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ITranslationService _translationService;

    public record ScaleOption(string TranslationKey, double Value)
    {
        public string DisplayName => (App.Current?.Services?.GetService(typeof(ITranslationService)) as ITranslationService)?.Translate(TranslationKey) ?? TranslationKey;
    }
    public record LanguageOption(string DisplayName, string Code);

    public ObservableCollection<ScaleOption> SettingsScaleOptions { get; } = new();
    
    public ObservableCollection<LanguageOption> SettingsLanguageOptions { get; } =
    [
        new("English", "en"),
        new("Polski", "pl")
    ];

    public ObservableCollection<string> SettingsWeightUnitOptions { get; } =
    [
        "kg",
        "lb"
    ];

    public ObservableCollection<string> SettingsDistanceUnitOptions { get; } =
    [
        "ft.",
        "m."
    ];

    [ObservableProperty]
    private double _uiScale;
    
    [ObservableProperty]
    private string _language = "en";
    
    [ObservableProperty]
    private string _weightUnit = "kg";

    [ObservableProperty]
    private string _distanceUnit = "ft.";

    private ScaleOption? _selectedScaleOption;
    public ScaleOption? SelectedScaleOption
    {
        get => _selectedScaleOption;
        set
        {
            if (SetProperty(ref _selectedScaleOption, value))
            {
                if (value == null) return;
                UiScale = value.Value;
                
                var currentSettings = _settingsService.LoadSettings();
                currentSettings.UiScale = value.Value;
                _settingsService.SaveSettings(currentSettings);
            }
        }
    }
    
    private LanguageOption _selectedLanguageOption;
    public LanguageOption SelectedLanguageOption
    {
        get => _selectedLanguageOption;
        set
        {
            if (SetProperty(ref _selectedLanguageOption, value))
            {
                if (value == null) return;
                Language = value.Code;
                _translationService.ChangeLanguage(value.Code);
                
                var currentSettings = _settingsService.LoadSettings();
                currentSettings.Language = value.Code;
                _settingsService.SaveSettings(currentSettings);
            }
        }
    }

    private string _selectedWeightUnitOption;
    public string SelectedWeightUnitOption
    {
        get => _selectedWeightUnitOption;
        set
        {
            if (SetProperty(ref _selectedWeightUnitOption, value))
            {
                if (string.IsNullOrEmpty(value)) return;
                WeightUnit = value;
                
                var currentSettings = _settingsService.LoadSettings();
                currentSettings.WeightUnit = value;
                _settingsService.SaveSettings(currentSettings);
            }
        }
    }

    private string _selectedDistanceUnitOption;
    public string SelectedDistanceUnitOption
    {
        get => _selectedDistanceUnitOption;
        set
        {
            if (SetProperty(ref _selectedDistanceUnitOption, value))
            {
                if (string.IsNullOrEmpty(value)) return;
                DistanceUnit = value;
                
                var currentSettings = _settingsService.LoadSettings();
                currentSettings.DistanceUnit = value;
                _settingsService.SaveSettings(currentSettings);
            }
        }
    }

    public SettingsTabViewModel(ISettingsService settingsService, ITranslationService translationService)
    {
        _settingsService = settingsService;
        _translationService = translationService;

        _translationService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ITranslationService.CurrentLanguage))
            {
                UpdateOptions();
            }
        };
        
        var settings = _settingsService.LoadSettings();
        
        UiScale = settings.UiScale;
        UpdateOptions();
        
        Language = settings.Language ?? "en";
        _selectedLanguageOption = SettingsLanguageOptions.FirstOrDefault(x => x.Code == Language) ?? SettingsLanguageOptions[0];

        WeightUnit = settings.WeightUnit ?? "kg";
        _selectedWeightUnitOption = SettingsWeightUnitOptions.FirstOrDefault(x => x == WeightUnit) ?? SettingsWeightUnitOptions[0];

        DistanceUnit = settings.DistanceUnit ?? "ft.";
        _selectedDistanceUnitOption = SettingsDistanceUnitOptions.FirstOrDefault(x => x == DistanceUnit) ?? SettingsDistanceUnitOptions[0];
    }

    private void UpdateOptions()
    {
        var oldScale = _selectedScaleOption?.Value ?? UiScale;

        SettingsScaleOptions.Clear();
        SettingsScaleOptions.Add(new("ui_scale_auto", 0.0));
        SettingsScaleOptions.Add(new("ui_scale_small", 0.75));
        SettingsScaleOptions.Add(new("ui_scale_medium", 1.0));
        SettingsScaleOptions.Add(new("ui_scale_large", 1.25));
        SettingsScaleOptions.Add(new("ui_scale_huge", 1.50));

        SelectedScaleOption = SettingsScaleOptions.FirstOrDefault(x => Math.Abs(x.Value - oldScale) < 0.01) ?? SettingsScaleOptions[2];
    }
}
