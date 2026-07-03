using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;

namespace DungeonApp.ViewModels;

public partial class CampaignDetailViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly CampaignService _campaignService = new();

    public Campaign Campaign { get; }

    // Właściwość trzymająca obecną, otwartą sesję
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveSession))]
    [NotifyPropertyChangedFor(nameof(CanAddNewSession))]
    private Session? _activeSession;

    // Lista sesji historycznych
    public ObservableCollection<Session> ArchivedSessions { get; } = new();

    // Pomocnicze flagi dla UI (ukrywanie/pokazywanie przycisków)
    public bool HasActiveSession => ActiveSession != null;
    public bool CanAddNewSession => ActiveSession == null;

    public CampaignDetailViewModel(Campaign campaign, MainWindowViewModel mainViewModel)
    {
        Campaign = campaign;
        _mainViewModel = mainViewModel;
        LoadSessions();
    }

    private void LoadSessions()
    {
        ArchivedSessions.Clear();
        ActiveSession = null;

        var allSessions = _campaignService.LoadSessions(Campaign.Id).OrderByDescending(s => s.Date).ToList();
        
        foreach (var session in allSessions)
        {
            if (!session.IsArchived && ActiveSession == null)
            {
                // Łapiemy najnowszą niearchiwizowaną jako aktywną
                ActiveSession = session;
            }
            else
            {
                // Reszta (lub starsze, które przypadkiem nie mają flagi) ląduje w archiwum
                session.IsArchived = true; 
                ArchivedSessions.Add(session);
            }
        }
    }

    [RelayCommand]
    private void Back()
    {
        _mainViewModel.CurrentView = null;
    }

    [RelayCommand]
    private void AddSession()
    {
        if (ActiveSession != null) return; // Podwójne zabezpieczenie

        var newSession = new Session
        {
            Id = Guid.NewGuid().ToString(),
            Number = ArchivedSessions.Count + 1,
            Title = $"Sesja {ArchivedSessions.Count + 1}",
            Date = DateTime.Now,
            IsArchived = false
        };

        _campaignService.SaveSession(Campaign.Id, newSession);
        ActiveSession = newSession;
        
        SyncCampaignStats();
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

        // Zmieniamy flagę i zapisujemy
        ActiveSession.IsArchived = true;
        _campaignService.SaveSession(Campaign.Id, ActiveSession);
        
        // Przenosimy wizualnie do archiwum
        ArchivedSessions.Insert(0, ActiveSession);
        ActiveSession = null;
        
        SyncCampaignStats();
    }

    [RelayCommand]
    private void OpenActiveSession()
    {
        if (ActiveSession == null) return;
        _mainViewModel.CurrentView = new SessionDetailViewModel(Campaign, ActiveSession, _mainViewModel);
    }

    [RelayCommand]
    private void ReadArchivedSession(Session? session)
    {
        if (session == null) return;
        _mainViewModel.CurrentView = new SessionDetailViewModel(Campaign, session, _mainViewModel);
    }

    private void SyncCampaignStats()
    {
        Campaign.SessionsCount = ArchivedSessions.Count + (ActiveSession != null ? 1 : 0);
        
        // Aktualizujemy datę ostatniej sesji na potrzeby głównego menu
        var latestDate = Campaign.CreatedAt;
        if (ActiveSession != null) latestDate = ActiveSession.Date;
        else if (ArchivedSessions.Any()) latestDate = ArchivedSessions.First().Date;
        
        Campaign.LastSession = latestDate;
        _campaignService.SaveCampaign(Campaign);
    }
}