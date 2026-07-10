using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Services;
using DungeonApp.ViewModels.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace DungeonApp.ViewModels;

public enum RegistryTab { Characters, Items, Adversaries }

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    public INavigationService NavigationService { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentSidebarWidth))]
    private bool _isSidebarExpanded = true;

    public double CurrentSidebarWidth => IsSidebarExpanded ? 260 : 74;


    private ViewModelBase? _dashboardCurrentView;
    public ViewModelBase? DashboardCurrentView
    {
        get => _dashboardCurrentView;
        set
        {
            if (SetProperty(ref _dashboardCurrentView, value))
            {
                OnPropertyChanged(nameof(ActiveMainView));
                OnPropertyChanged(nameof(IsCampaignsActive));
                OnPropertyChanged(nameof(IsSettingsActive));
                OnPropertyChanged(nameof(IsCharactersActive));
                OnPropertyChanged(nameof(IsItemsActive));
                OnPropertyChanged(nameof(IsAdversariesActive));
            }
        }
    }

    public bool IsCampaignsActive => DashboardCurrentView is CampaignsTabViewModel;
    public bool IsSettingsActive => DashboardCurrentView is SettingsTabViewModel;
    public bool IsCharactersActive => DashboardCurrentView is CharactersTabViewModel;
    public bool IsItemsActive => DashboardCurrentView is ItemsTabViewModel;
    public bool IsAdversariesActive => DashboardCurrentView is AdversariesTabViewModel;

    public ViewModelBase? ActiveMainView => NavigationService.CurrentView ?? DashboardCurrentView;

    public bool IsDashboardVisible => NavigationService.CurrentView == null;

    [ObservableProperty]
    private double _uiScale = 1.0;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScaledMinWidth))]
    [NotifyPropertyChangedFor(nameof(ScaledMinHeight))]
    private double _effectiveUiScale = 1.0;

    public double ScaledMinWidth => 900 * EffectiveUiScale;
    public double ScaledMinHeight => 600 * EffectiveUiScale;

    public MainWindowViewModel(
        INavigationService navigationService,
        ISettingsService settingsService)
    {
        NavigationService = navigationService;
        _settingsService = settingsService;

        ((System.ComponentModel.INotifyPropertyChanged)NavigationService).PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(NavigationService.CurrentView))
            {
                OnPropertyChanged(nameof(IsDashboardVisible));
                OnPropertyChanged(nameof(ActiveMainView));
            }
        };

        var settings = _settingsService.LoadSettings();
        UiScale = settings.UiScale;

        _settingsService.SettingsChanged += () =>
        {
            var newSettings = _settingsService.LoadSettings();
            UiScale = newSettings.UiScale;
        };

        // Start with Campaigns tab
        ShowCampaigns();
    }

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

    [RelayCommand]
    private void ShowCampaigns() 
    { 
        NavigationService.ClearNavigation(); 
        DashboardCurrentView = App.Current!.Services!.GetRequiredService<CampaignsTabViewModel>(); 
    }

    [RelayCommand]
    private void SelectRegistryTab(RegistryTab tab) 
    { 
        NavigationService.ClearNavigation(); 
        DashboardCurrentView = tab switch
        {
            RegistryTab.Characters => App.Current!.Services!.GetRequiredService<CharactersTabViewModel>(),
            RegistryTab.Items => App.Current!.Services!.GetRequiredService<ItemsTabViewModel>(),
            RegistryTab.Adversaries => App.Current!.Services!.GetRequiredService<AdversariesTabViewModel>(),
            _ => App.Current!.Services!.GetRequiredService<CharactersTabViewModel>()
        };
    }

    [RelayCommand]
    private void ShowSettings() 
    { 
        NavigationService.ClearNavigation(); 
        DashboardCurrentView = App.Current!.Services!.GetRequiredService<SettingsTabViewModel>(); 
    }
}
