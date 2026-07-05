using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DungeonApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ICampaignService _campaignService;
    private readonly ICharacterService _characterService;
    public INavigationService NavigationService { get; }

    public ObservableCollection<Campaign> Campaigns { get; } = new();
    public ObservableCollection<PlayerCharacter> Characters { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCampaignsListVisible))]
    private bool _isCampaignsVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCampaignsGridView))]
    private bool _isCampaignsListView = true;

    public bool IsCampaignsGridView => !IsCampaignsListView;

    [RelayCommand]
    private void SetCampaignsViewMode(string mode) => IsCampaignsListView = mode == "List";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCharactersListVisible))]
    private bool _isCharactersVisible = false;

    [ObservableProperty]
    private bool _isSettingsVisible = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentSidebarWidth))]
    private bool _isSidebarExpanded = true;

    /// <summary>
    /// Zwraca aktualną szerokość paska bocznego (rozwinięty vs zwinięty).
    /// </summary>
    /// <remarks>
    /// **Dlaczego 74 piksele w trybie zwiniętym (a nie np. 60)?**
    /// W pliku `NavigationBarView.axaml` główny kontener paska ma <c>Margin="12"</c> z lewej i prawej strony (co pochłania 24px).
    /// Same przyciski wewnątrz (klasa <c>navButton</c>) wymagają absolutnego minimum 50px (20px ikona + 15px paddingu z lewej i prawej).
    /// 50px + 24px marginesu = 74px. Ustawienie mniejszej szerokości powoduje obcinanie lewej krawędzi przycisków przez Avalonię.
    /// </remarks>
    public double CurrentSidebarWidth => IsSidebarExpanded ? 260 : 74;

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

    public bool IsCampaignsListVisible => IsCampaignsVisible && 
        !(NavigationService.CurrentView is CampaignDetailViewModel || NavigationService.CurrentView is SessionDetailViewModel || NavigationService.CurrentView is CreateCampaignViewModel);
        
    public bool IsCharactersListVisible => IsCharactersVisible && 
        !(NavigationService.CurrentView is CreateCharacterViewModel);
    
    public bool IsActiveViewVisible 
    {
        get 
        {
            if (NavigationService.CurrentView == null) return false;
            if (NavigationService.CurrentView is CreateCharacterViewModel) return IsCharactersVisible;
            return IsCampaignsVisible; // CampaignDetailViewModel, SessionDetailViewModel, CreateCampaignViewModel
        }
    }

    [ObservableProperty]
    private double _uiScale = 1.0;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScaledMinWidth))]
    [NotifyPropertyChangedFor(nameof(ScaledMinHeight))]
    private double _effectiveUiScale = 1.0;

    public double ScaledMinWidth => 900 * EffectiveUiScale;
    public double ScaledMinHeight => 600 * EffectiveUiScale;

    public record ScaleOption(string DisplayName, double Value);

    public ObservableCollection<ScaleOption> SettingsScaleOptions { get; } =
    [
        new("Auto", 0.0),
        new("Mały (75%)", 0.75),
        new("Średni (100%)", 1.0),
        new("Duży (125%)", 1.25),
        new("Ogromny (150%)", 1.5)
    ];

    [ObservableProperty]
    private ScaleOption _selectedScaleOption;

    [ObservableProperty]
    private PlayerCharacter? _selectedCharacter;

    [ObservableProperty]
    private CharacterDetailViewModel? _selectedCharacterDetail;

    public MainWindowViewModel(
        ICampaignService campaignService, 
        ICharacterService characterService,
        INavigationService navigationService)
    {
        _campaignService = campaignService;
        _characterService = characterService;
        NavigationService = navigationService;

        SelectedScaleOption = SettingsScaleOptions[2]; // Średni (100%) domyślnie (sweet spot dla 1440p)

        LoadCampaignsFromDisk();
        LoadCharactersFromDisk();

        ((System.ComponentModel.INotifyPropertyChanged)NavigationService).PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(NavigationService.CurrentView))
            {
                OnPropertyChanged(nameof(IsCampaignsListVisible));
                OnPropertyChanged(nameof(IsCharactersListVisible));
                OnPropertyChanged(nameof(IsActiveViewVisible));
            }
        };
    }

    partial void OnSelectedCharacterChanged(PlayerCharacter? value)
    {
        if (value != null)
        {
            SelectedCharacterDetail = ActivatorUtilities.CreateInstance<CharacterDetailViewModel>(
                App.Current!.Services!, value);
        }
        else
        {
            SelectedCharacterDetail = null;
        }
    }

    partial void OnSelectedScaleOptionChanged(ScaleOption value)
    {
        if (value != null)
        {
            UiScale = value.Value;
        }
    }

    private void LoadCampaignsFromDisk()
    {
        foreach (var campaign in _campaignService.LoadAllCampaigns())
            Campaigns.Add(campaign);
    }

    public void RefreshCharactersList(string? keepSelectedId = null)
    {
        Characters.Clear();
        PlayerCharacter? newSelected = null;
        foreach (var character in _characterService.LoadAllCharacters())
        {
            Characters.Add(character);
            if (character.Id == keepSelectedId)
                newSelected = character;
        }
        SelectedCharacter = newSelected;
    }

    public void LoadCharactersFromDisk()
    {
        RefreshCharactersList(null);
    }

    [RelayCommand]
    private void ShowCampaigns() { NavigationService.ClearNavigation(); IsCampaignsVisible = true; IsCharactersVisible = false; IsSettingsVisible = false; OnPropertyChanged(nameof(IsActiveViewVisible)); }

    [RelayCommand]
    private void ShowCharacters() { NavigationService.ClearNavigation(); IsCampaignsVisible = false; IsCharactersVisible = true; IsSettingsVisible = false; OnPropertyChanged(nameof(IsActiveViewVisible)); }

    [RelayCommand]
    private void ShowSettings() { NavigationService.ClearNavigation(); IsCampaignsVisible = false; IsCharactersVisible = false; IsSettingsVisible = true; OnPropertyChanged(nameof(IsActiveViewVisible)); }
    
    [RelayCommand]
    private void RefreshCampaigns() { Campaigns.Clear(); LoadCampaignsFromDisk(); }
    
    [RelayCommand]
    private void CreateNewCampaign()
    {
        var vm = App.Current!.Services!.GetRequiredService<CreateCampaignViewModel>();
        NavigationService.NavigateTo(vm);
    }
    
    [RelayCommand]
    private void DeleteCampaign(Campaign campaign)
    {
        _campaignService.DeleteCampaign(campaign.Id);
        Campaigns.Remove(campaign);
    }
    
    [RelayCommand]
    private void OpenCampaign(Campaign campaign)
    {
        // Factory pattern could be better, but for now we'll inject dependencies manually 
        // or resolve from DI. Since it needs `Campaign`, we can use ActivatorUtilities
        var vm = ActivatorUtilities.CreateInstance<CampaignDetailViewModel>(App.Current!.Services!, campaign);
        NavigationService.NavigateTo(vm);
    }

    [RelayCommand]
    private void RefreshCharacters() => LoadCharactersFromDisk();

    [RelayCommand]
    private void CreateNewCharacter()
    {
        var vm = App.Current!.Services!.GetRequiredService<CreateCharacterViewModel>();
        NavigationService.NavigateTo(vm);
    }

    [RelayCommand]
    private void SelectCharacter(PlayerCharacter character) => SelectedCharacter = character;

}