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

    public ObservableCollection<Session> Sessions { get; } = new();

    [ObservableProperty]
    private Session? _selectedSession;

    public CampaignDetailViewModel(Campaign campaign, MainWindowViewModel mainViewModel)
    {
        Campaign = campaign;
        _mainViewModel = mainViewModel;
        LoadSessions();
    }

    private void LoadSessions()
    {
        Sessions.Clear();
        foreach (var session in _campaignService.LoadSessions(Campaign.Id))
            Sessions.Add(session);
    }

    [RelayCommand]
    private void Back()
    {
        _mainViewModel.CurrentView = null;
    }

    [RelayCommand]
    private void AddSession()
    {
        var newSession = new Session
        {
            Number = Sessions.Count + 1,
            Title = $"Sesja {Sessions.Count + 1}",
            Date = DateTime.Now,
            Notes = string.Empty
        };

        _campaignService.SaveSession(Campaign.Id, newSession);
        Sessions.Insert(0, newSession);
        SelectedSession = newSession;

        SyncCampaignStats();
    }

    [RelayCommand]
    private void DeleteSession(Session? session)
    {
        if (session == null) return;

        _campaignService.DeleteSession(Campaign.Id, session.Id);
        Sessions.Remove(session);

        if (SelectedSession == session)
            SelectedSession = null;

        SyncCampaignStats();
    }

    [RelayCommand]
    private void SelectSession(Session session)
    {
        SelectedSession = session;
    }

    partial void OnSelectedSessionChanged(Session? oldValue, Session? newValue)
    {
        // Zapisujemy poprzednio otwartą sesję (np. notatki), zanim przełączymy się na inną
        if (oldValue != null)
            _campaignService.SaveSession(Campaign.Id, oldValue);
    }

    [RelayCommand]
    private void SaveCurrentSession()
    {
        if (SelectedSession != null)
        {
            _campaignService.SaveSession(Campaign.Id, SelectedSession);
            SyncCampaignStats();
        }
    }

    private void SyncCampaignStats()
    {
        Campaign.SessionsCount = Sessions.Count;
        Campaign.LastSession = Sessions.Count > 0 ? Sessions.Max(s => s.Date) : Campaign.CreatedAt;
        _campaignService.SaveCampaign(Campaign);
    }
}