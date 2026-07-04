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
    private bool _isCampaignsVisible = true;

    [ObservableProperty]
    private bool _isCharactersVisible = false;

    [ObservableProperty]
    private bool _isSettingsVisible = false;

    [ObservableProperty]
    private double _uiScale = 1.0;
    
    [ObservableProperty]
    private double _effectiveUiScale = 1.0;

    public record ScaleOption(string DisplayName, double Value);

    public ObservableCollection<ScaleOption> SettingsScaleOptions { get; } = new()
    {
        new ScaleOption("Auto (Responsywnie)", 0.0),
        new ScaleOption("Mały (75%)", 0.75),
        new ScaleOption("Normalny (100%)", 1.0),
        new ScaleOption("Duży (125%)", 1.25),
        new ScaleOption("Bardzo Duży (150%)", 1.5)
    };

    [ObservableProperty]
    private ScaleOption _selectedScaleOption;

    [ObservableProperty]
    private PlayerCharacter? _selectedCharacter;

    public MainWindowViewModel(
        ICampaignService campaignService, 
        ICharacterService characterService,
        INavigationService navigationService)
    {
        _campaignService = campaignService;
        _characterService = characterService;
        NavigationService = navigationService;

        SelectedScaleOption = SettingsScaleOptions[0]; // Auto domyślnie

        LoadCampaignsFromDisk();
        LoadCharactersFromDisk();
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

    public void LoadCharactersFromDisk()
    {
        Characters.Clear();
        foreach (var character in _characterService.LoadAllCharacters())
            Characters.Add(character);
        
        SelectedCharacter = null;
    }

    [RelayCommand]
    private void ShowCampaigns() { IsCampaignsVisible = true; IsCharactersVisible = false; IsSettingsVisible = false; }

    [RelayCommand]
    private void ShowCharacters() { IsCampaignsVisible = false; IsCharactersVisible = true; IsSettingsVisible = false; }

    [RelayCommand]
    private void ShowSettings() { IsCampaignsVisible = false; IsCharactersVisible = false; IsSettingsVisible = true; }
    
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

    [RelayCommand]
    private void OpenCharacterDetail(PlayerCharacter character)
    {
        if (character == null) return;
        var vm = ActivatorUtilities.CreateInstance<CharacterDetailViewModel>(App.Current!.Services!, character);
        NavigationService.NavigateTo(vm);
    }

    [RelayCommand]
    private void DeleteCharacter(PlayerCharacter? character)
    {
        if (character == null) return;
        _characterService.DeleteCharacter(character.Id);
        Characters.Remove(character);
        if (SelectedCharacter == character) SelectedCharacter = null;
    }
}