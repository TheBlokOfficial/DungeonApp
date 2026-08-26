using System;
using System.Threading.Tasks;
using System.Windows.Input;
using DungeonApp.Desktop.Themes;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Settings;

/// <summary>Presentation state for the sidebar's "Szybkie ustawienia" flyout (scale profile picker).</summary>
public sealed class QuickSettingsViewModel : ObservableObject
{
    private readonly Action<UiScaleProfile> _onProfileSelected;
    private UiScaleProfile _selectedProfile;

    public QuickSettingsViewModel(UiScaleProfile initialProfile, Action<UiScaleProfile> onProfileSelected)
    {
        _selectedProfile = initialProfile;
        _onProfileSelected = onProfileSelected;

        SelectSmallCommand = new AsyncCommand(() => Select(UiScaleProfile.Small));
        SelectMediumCommand = new AsyncCommand(() => Select(UiScaleProfile.Medium));
        SelectLargeCommand = new AsyncCommand(() => Select(UiScaleProfile.Large));
    }

    public UiScaleProfile SelectedProfile
    {
        get => _selectedProfile;
        private set
        {
            if (!SetField(ref _selectedProfile, value))
            {
                return;
            }

            RaisePropertyChanged(nameof(IsSmallSelected));
            RaisePropertyChanged(nameof(IsMediumSelected));
            RaisePropertyChanged(nameof(IsLargeSelected));
        }
    }

    public bool IsSmallSelected => SelectedProfile == UiScaleProfile.Small;

    public bool IsMediumSelected => SelectedProfile == UiScaleProfile.Medium;

    public bool IsLargeSelected => SelectedProfile == UiScaleProfile.Large;

    public ICommand SelectSmallCommand { get; }

    public ICommand SelectMediumCommand { get; }

    public ICommand SelectLargeCommand { get; }

    private Task Select(UiScaleProfile profile)
    {
        SelectedProfile = profile;
        _onProfileSelected(profile);
        return Task.CompletedTask;
    }
}
