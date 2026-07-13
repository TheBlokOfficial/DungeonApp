using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;
using DungeonApp.ViewModels.Campaigns.Tabs;

namespace DungeonApp.ViewModels;

public partial class CampaignDetailViewModel : ViewModelBase
{
    private readonly ICampaignService _campaignService;
    private readonly ICharacterService _characterService;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private bool _isEditingName;

    [ObservableProperty]
    private ViewModelBase? _activeTabContent;

    [ObservableProperty]
    private string _activeTabName = "Dashboard";

    [ObservableProperty]
    private bool _isHudVisible = true;

    public Campaign Campaign { get; }
    public ObservableCollection<PlayerCharacter> CampaignCharacters { get; } = new();

    public CampaignDetailViewModel(
        Campaign campaign,
        ICampaignService campaignService,
        ICharacterService characterService,
        INavigationService navigationService,
        IServiceProvider serviceProvider)
    {
        Campaign = campaign;
        _campaignService = campaignService;
        _characterService = characterService;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;

        NavigateToDashboard();
    }

    public override async void OnNavigatedTo()
    {
        try
        {
            await LoadCampaignCharactersAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CampaignDetail] Błąd ładowania postaci kampanii: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.NavigateBack();
    }

    [RelayCommand]
    private void ToggleHud()
    {
        IsHudVisible = !IsHudVisible;
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        ActiveTabContent = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<CampaignDashboardViewModel>(_serviceProvider, Campaign);
        ActiveTabName = "Dashboard";
    }

    [RelayCommand]
    private void NavigateToTracker()
    {
        ActiveTabContent = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<CampaignTrackerViewModel>(_serviceProvider, Campaign);
        ActiveTabName = "Tracker";
    }

    [RelayCommand]
    private void NavigateToNotes()
    {
        ActiveTabContent = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<CampaignNotesViewModel>(_serviceProvider, Campaign);
        ActiveTabName = "Notes";
    }

    [RelayCommand]
    private void NavigateToStory()
    {
        ActiveTabContent = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<CampaignStoryViewModel>(_serviceProvider, Campaign);
        ActiveTabName = "Story";
    }

    [RelayCommand]
    private void StartEditingName()
    {
        IsEditingName = true;
    }

    [RelayCommand]
    private void SaveCampaignName()
    {
        IsEditingName = false;
        if (string.IsNullOrWhiteSpace(Campaign.Name))
        {
            Campaign.Name = "Nienazwana Kampania";
        }
        _campaignService.SaveCampaign(Campaign);
    }

    public async Task LoadCampaignCharactersAsync()
    {
        var allCharacters = await Task.Run(() => _characterService.LoadAllCharacters());
        
        CampaignCharacters.Clear();
        foreach (var id in Campaign.CharacterIds)
        {
            var character = allCharacters.FirstOrDefault(c => c.Id == id);
            if (character != null) CampaignCharacters.Add(character);
        }
    }

    [RelayCommand]
    private void OpenCreateSession()
    {
        var vm = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<CreateSessionViewModel>(App.Current!.Services!, Campaign);
        _navigationService.ShowOverlay(vm);
    }

    [RelayCommand]
    private void AddCharacterToCampaign()
    {
        var vm = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<AddCharacterToCampaignViewModel>(_serviceProvider, Campaign);
        _navigationService.ShowOverlay(vm);
    }

    [RelayCommand]
    private void RemoveCharacterFromCampaign(PlayerCharacter character)
    {
        Campaign.CharacterIds.Remove(character.Id);
        _campaignService.SaveCampaign(Campaign);
        CampaignCharacters.Remove(character);
    }
}
