using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DungeonApp.ViewModels;

public partial class CampaignDetailViewModel : ViewModelBase
{
    private readonly ICampaignService _campaignService;
    private readonly ICharacterService _characterService;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveSession))]
    [NotifyPropertyChangedFor(nameof(CanAddNewSession))]
    private Session? _activeSession;

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

        LoadSessions();
        LoadCampaignCharacters();
    }

    public Campaign Campaign { get; }
    public ObservableCollection<Session> ArchivedSessions { get; } = new();

    public bool HasActiveSession => ActiveSession != null;
    public bool CanAddNewSession => ActiveSession == null;
    public ObservableCollection<PlayerCharacter> CampaignCharacters { get; } = new();

    private void LoadSessions()
    {
        ArchivedSessions.Clear();
        ActiveSession = null;

        var allSessions = _campaignService.LoadSessions(Campaign.Id).OrderByDescending(s => s.Date).ToList();

        foreach (var session in allSessions)
        {
            if (!session.IsArchived && ActiveSession == null)
            {
                ActiveSession = session;
            }
            else
            {
                session.IsArchived = true;
                ArchivedSessions.Add(session);
            }
        }
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.NavigateBack();
    }

    [RelayCommand]
    private void AddSession()
    {
        if (ActiveSession != null) return;
        var vm = ActivatorUtilities.CreateInstance<CreateSessionViewModel>(_serviceProvider, Campaign);
        _navigationService.NavigateTo(vm);
    }

    [RelayCommand]
    private void DeleteActiveSession()
    {
        if (ActiveSession == null) return;

        _campaignService.DeleteSession(Campaign.Id, ActiveSession.Id);
        ActiveSession = null;

        SyncCampaignStats();
    }

    [RelayCommand]
    private void ArchiveActiveSession()
    {
        if (ActiveSession == null) return;

        ActiveSession.IsArchived = true;
        _campaignService.SaveSession(Campaign.Id, ActiveSession);

        ArchivedSessions.Insert(0, ActiveSession);
        ActiveSession = null;

        SyncCampaignStats();
    }

    [RelayCommand]
    private void OpenActiveSession()
    {
        if (ActiveSession == null) return;
        var vm = ActivatorUtilities.CreateInstance<SessionDetailViewModel>(_serviceProvider, Campaign, ActiveSession);
        _navigationService.NavigateTo(vm);
    }

    [RelayCommand]
    private void ReadArchivedSession(Session? session)
    {
        if (session == null) return;
        var vm = ActivatorUtilities.CreateInstance<SessionDetailViewModel>(_serviceProvider, Campaign, session);
        _navigationService.NavigateTo(vm);
    }

    private void SyncCampaignStats()
    {
        Campaign.SessionsCount = ArchivedSessions.Count + (ActiveSession != null ? 1 : 0);

        var latestDate = Campaign.CreatedAt;
        if (ActiveSession != null) latestDate = ActiveSession.Date;
        else if (ArchivedSessions.Any()) latestDate = ArchivedSessions.First().Date;

        Campaign.LastSession = latestDate;
        _campaignService.SaveCampaign(Campaign);
    }

    private void LoadCampaignCharacters()
    {
        CampaignCharacters.Clear();
        var allCharacters = _characterService.LoadAllCharacters();

        foreach (var id in Campaign.CharacterIds)
        {
            var character = allCharacters.FirstOrDefault(c => c.Id == id);
            if (character != null) CampaignCharacters.Add(character);
        }
    }

    [RelayCommand]
    private void AddCharacterToCampaign()
    {
        var vm = ActivatorUtilities.CreateInstance<AddCharacterToCampaignViewModel>(_serviceProvider, Campaign);
        _navigationService.NavigateTo(vm);
    }

    [RelayCommand]
    private void RemoveCharacterFromCampaign(PlayerCharacter character)
    {
        Campaign.CharacterIds.Remove(character.Id);
        _campaignService.SaveCampaign(Campaign);
        CampaignCharacters.Remove(character);
    }
}
