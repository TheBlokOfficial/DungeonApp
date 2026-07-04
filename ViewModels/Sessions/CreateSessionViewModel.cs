using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DungeonApp.ViewModels;

public partial class CharacterSelectionItem : ObservableObject
{
    public PlayerCharacter Character { get; }
    
    [ObservableProperty]
    private bool _isSelected;

    public CharacterSelectionItem(PlayerCharacter character)
    {
        Character = character;
        IsSelected = true;
    }
}

public partial class CreateSessionViewModel : ViewModelBase
{
    private readonly Campaign _campaign;
    private readonly INavigationService _navigationService;
    private readonly ICampaignService _campaignService;
    private readonly ICharacterService _characterService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _title = string.Empty;

    public ObservableCollection<CharacterSelectionItem> AvailableCharacters { get; } = new();

    public CreateSessionViewModel(
        Campaign campaign,
        INavigationService navigationService,
        ICampaignService campaignService,
        ICharacterService characterService,
        IServiceProvider serviceProvider)
    {
        _campaign = campaign;
        _navigationService = navigationService;
        _campaignService = campaignService;
        _characterService = characterService;
        _serviceProvider = serviceProvider;
        
        var nextSessionNumber = campaign.SessionsCount + 1;
        Title = $"Sesja {nextSessionNumber}";

        LoadCampaignCharacters();
    }

    private void LoadCampaignCharacters()
    {
        var allCharacters = _characterService.LoadAllCharacters();
        var campaignCharacters = allCharacters.Where(c => _campaign.CharacterIds.Contains(c.Id));

        foreach (var character in campaignCharacters)
        {
            AvailableCharacters.Add(new CharacterSelectionItem(character));
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        var vm = ActivatorUtilities.CreateInstance<CampaignDetailViewModel>(_serviceProvider, _campaign);
        _navigationService.NavigateTo(vm);
    }

    [RelayCommand]
    private void Create()
    {
        if (string.IsNullOrWhiteSpace(Title)) return;

        var participatingIds = AvailableCharacters
            .Where(c => c.IsSelected)
            .Select(c => c.Character.Id)
            .ToList();

        var newSession = new Session
        {
            Id = Guid.NewGuid().ToString(),
            Number = _campaign.SessionsCount + 1,
            Title = Title.Trim(),
            Date = DateTime.Now,
            IsArchived = false,
            ParticipatingCharacterIds = participatingIds
        };

        _campaignService.SaveSession(_campaign.Id, newSession);

        _campaign.SessionsCount += 1;
        _campaign.LastSession = newSession.Date;
        _campaignService.SaveCampaign(_campaign);

        var vm = ActivatorUtilities.CreateInstance<CampaignDetailViewModel>(_serviceProvider, _campaign);
        _navigationService.NavigateTo(vm);
    }
}