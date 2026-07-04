using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DungeonApp.ViewModels;

public partial class AddCharacterToCampaignViewModel : ViewModelBase
{
    private readonly Campaign _campaign;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICharacterService _characterService;
    private readonly ICampaignService _campaignService;

    public ObservableCollection<CharacterSelectionItem> AvailableCharacters { get; } = new();

    public AddCharacterToCampaignViewModel(
        Campaign campaign, 
        INavigationService navigationService,
        IServiceProvider serviceProvider,
        ICharacterService characterService,
        ICampaignService campaignService)
    {
        _campaign = campaign;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        _characterService = characterService;
        _campaignService = campaignService;
        
        LoadAvailableCharacters();
    }

    private void LoadAvailableCharacters()
    {
        var allCharacters = _characterService.LoadAllCharacters();
        var charactersToAdd = allCharacters.Where(c => !_campaign.CharacterIds.Contains(c.Id));

        foreach (var character in charactersToAdd)
        {
            AvailableCharacters.Add(new CharacterSelectionItem(character) { IsSelected = false }); 
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.NavigateBack();
    }

    [RelayCommand]
    private void Save()
    {
        var selectedIds = AvailableCharacters
            .Where(c => c.IsSelected)
            .Select(c => c.Character.Id)
            .ToList();

        if (selectedIds.Any())
        {
            _campaign.CharacterIds.AddRange(selectedIds);
            _campaignService.SaveCampaign(_campaign);
        }

        _navigationService.NavigateBack();
    }
}