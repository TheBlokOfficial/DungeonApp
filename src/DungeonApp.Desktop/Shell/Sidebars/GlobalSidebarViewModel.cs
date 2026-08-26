using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using DungeonApp.Desktop.Settings;
using DungeonApp.Desktop.Themes;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell.Sidebars;

public sealed class GlobalSidebarViewModel : ObservableObject
{
    private readonly Action<AppSettings> _applyAndPersist;
    private AppSettings _settings;

    public GlobalSidebarViewModel(AppSettings settings, Action<AppSettings> applyAndPersist)
    {
        _settings = settings;
        _applyAndPersist = applyAndPersist;

        LibraryItems =
        [
            new NavigationItemViewModel("DungeonIconBookOpen", "Kampanie", true),
            new NavigationItemViewModel("DungeonIconUsers", "Bohaterowie"),
            new NavigationItemViewModel("DungeonIconBoxes", "Paczki zawartości")
        ];
        SettingsItem = new NavigationItemViewModel("DungeonIconSettings", "Ustawienia");
        QuickSettings = new QuickSettingsViewModel(_settings.ScaleProfile, OnProfileSelected);
        ToggleSidebarVariantCommand = new AsyncCommand(ToggleSidebarVariant);
    }

    public IReadOnlyList<NavigationItemViewModel> LibraryItems { get; }

    public NavigationItemViewModel SettingsItem { get; }

    public QuickSettingsViewModel QuickSettings { get; }

    public SidebarVariant CurrentVariant => _settings.SidebarVariant;

    public bool IsCompact => CurrentVariant == SidebarVariant.Compact;

    public string ToggleActionLabel => IsCompact ? "Rozwiń sidebar" : "Zwiń sidebar";

    public ICommand ToggleSidebarVariantCommand { get; }

    private void OnProfileSelected(UiScaleProfile profile)
    {
        _settings = _settings with { ScaleProfile = profile };
        _applyAndPersist(_settings);
    }

    private Task ToggleSidebarVariant()
    {
        _settings = _settings with
        {
            SidebarVariant = _settings.SidebarVariant == SidebarVariant.Full ? SidebarVariant.Compact : SidebarVariant.Full
        };
        _applyAndPersist(_settings);
        RaisePropertyChanged(nameof(CurrentVariant));
        RaisePropertyChanged(nameof(IsCompact));
        RaisePropertyChanged(nameof(ToggleActionLabel));
        return Task.CompletedTask;
    }
}
