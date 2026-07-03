using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;

namespace DungeonApp.ViewModels;

public partial class SessionDetailViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly Campaign _campaign;
    private readonly CampaignService _campaignService = new();

    [ObservableProperty]
    private Session _session;

    [ObservableProperty]
    private string _newNoteText = string.Empty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    private readonly ObservableCollection<Note> _allNotes = new();
    public ObservableCollection<Note> FilteredNotes { get; } = new();

    public bool IsReadOnly => Session.IsArchived;
    public bool IsEditable => !Session.IsArchived;

    public SessionDetailViewModel(Campaign campaign, Session session, MainWindowViewModel mainViewModel)
    {
        _campaign = campaign;
        Session = session;
        _mainViewModel = mainViewModel;
        
        var sortedNotes = session.NotesList.OrderByDescending(n => n.CreatedAt);
        foreach (var n in sortedNotes)
        {
            _allNotes.Add(n);
        }
        
        RefreshFilteredNotes();
    }

    partial void OnSearchQueryChanged(string value) => RefreshFilteredNotes();

    private void RefreshFilteredNotes()
    {
        FilteredNotes.Clear();
        var query = SearchQuery?.ToLowerInvariant() ?? "";
        var filtered = _allNotes.Where(n => string.IsNullOrWhiteSpace(query) || n.Text.ToLowerInvariant().Contains(query));

        foreach (var n in filtered)
            FilteredNotes.Add(n);
    }

    [RelayCommand]
    private void AddNote()
    {
        if (string.IsNullOrWhiteSpace(NewNoteText)) return;

        var note = new Note { Text = NewNoteText.Trim(), CreatedAt = DateTime.Now };

        _allNotes.Insert(0, note);
        Session.NotesList.Add(note);
        Save();
        
        NewNoteText = string.Empty;
        RefreshFilteredNotes();
    }

    [RelayCommand]
    private void DeleteNote(Note note)
    {
        if (note == null) return;
        _allNotes.Remove(note);
        Session.NotesList.Remove(note);
        Save();
        RefreshFilteredNotes();
    }

    [RelayCommand]
    private void EditNoteStart(Note note) { note.DraftText = note.Text; note.IsEditing = true; }

    [RelayCommand]
    private void EditNoteCancel(Note note) { note.IsEditing = false; }

    [RelayCommand]
    private void EditNoteSave(Note note) { note.Text = note.DraftText.Trim(); note.IsEditing = false; Save(); }

    private void Save() => _campaignService.SaveSession(_campaign.Id, Session);

    [RelayCommand]
    private void Back()
    {
        Save();
        _mainViewModel.CurrentView = new CampaignDetailViewModel(_campaign, _mainViewModel);
    }
}