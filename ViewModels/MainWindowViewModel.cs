using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;

namespace DungeonApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase? _currentView;
    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }
    
    private readonly CampaignService _campaignService = new();
    private readonly CharacterService _characterService = new();

    public ObservableCollection<Campaign> Campaigns { get; } = new();
    public ObservableCollection<PlayerCharacter> Characters { get; } = new();

    [ObservableProperty]
    private bool _isCampaignsVisible = true;

    [ObservableProperty]
    private bool _isCharactersVisible = false;

    // Aktualnie podglądana postać w prawym panelu
    [ObservableProperty]
    private PlayerCharacter? _selectedCharacter;

    public MainWindowViewModel()
    {
        LoadCampaignsFromDisk();
        LoadCharactersFromDisk();
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

    // --- NAWIGACJA MENU ---

    [RelayCommand]
    private void ShowCampaigns() { IsCampaignsVisible = true; IsCharactersVisible = false; }

    [RelayCommand]
    private void ShowCharacters() { IsCampaignsVisible = false; IsCharactersVisible = true; }
    
    // --- KAMPANIE ---

    [RelayCommand]
    private void RefreshCampaigns() { Campaigns.Clear(); LoadCampaignsFromDisk(); }
    
    [RelayCommand]
    private void CreateNewCampaign() => CurrentView = new CreateCampaignViewModel(this);
    
    [RelayCommand]
    private void DeleteCampaign(Campaign campaign)
    {
        _campaignService.DeleteCampaign(campaign.Id);
        Campaigns.Remove(campaign);
    }
    
    [RelayCommand]
    private void OpenCampaign(Campaign campaign) => CurrentView = new CampaignDetailViewModel(campaign, this);

    // --- BOHATEROWIE ---

    [RelayCommand]
    private void RefreshCharacters() => LoadCharactersFromDisk();

    [RelayCommand]
    private void CreateNewCharacter() => CurrentView = new CreateCharacterViewModel(this);

    [RelayCommand]
    private void SelectCharacter(PlayerCharacter character) => SelectedCharacter = character;

    [RelayCommand]
    private void DeleteCharacter(PlayerCharacter? character)
    {
        if (character == null) return;
        _characterService.DeleteCharacter(character.Id);
        Characters.Remove(character);
        if (SelectedCharacter == character) SelectedCharacter = null;
    }
}