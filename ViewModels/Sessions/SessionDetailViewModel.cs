using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DungeonApp.ViewModels;

public partial class SessionDetailViewModel : ViewModelBase
{
    private readonly Campaign _campaign;
    private readonly ICampaignService _campaignService;
    private readonly ICharacterService _characterService;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private Session _session;

    [ObservableProperty]
    private string _newNoteText = string.Empty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    private readonly ObservableCollection<Note> _allNotes = new();
    public ObservableCollection<Note> FilteredNotes { get; } = new();

    public ObservableCollection<Combatant> CombatantsList { get; } = new();
    public ObservableCollection<PlayerCharacter> PartyCharacters { get; } = new();

    [ObservableProperty]
    private string _newCombatantName = string.Empty;

    [ObservableProperty]
    private int _newCombatantInitiative;

    [ObservableProperty]
    private int _newCombatantHp = 10;

    [ObservableProperty]
    private bool _newCombatantIsEnemy = true;

    [ObservableProperty]
    private int _currentRound = 1;

    [ObservableProperty]
    private bool _isCombatActive;

    public bool IsReadOnly => Session.IsArchived;
    public bool IsEditable => !Session.IsArchived;

    public SessionDetailViewModel(
        Campaign campaign, 
        Session session,
        ICampaignService campaignService,
        ICharacterService characterService,
        INavigationService navigationService,
        IServiceProvider serviceProvider)
    {
        _campaign = campaign;
        Session = session;
        _campaignService = campaignService;
        _characterService = characterService;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        
        var sortedNotes = session.NotesList.OrderByDescending(n => n.CreatedAt);
        foreach (var n in sortedNotes)
        {
            _allNotes.Add(n);
        }
        
        RefreshFilteredNotes();

        var sortedCombatants = session.Combatants.OrderByDescending(c => c.Initiative);
        foreach (var c in sortedCombatants)
        {
            CombatantsList.Add(c);
        }

        IsCombatActive = CombatantsList.Any(c => c.IsActiveTurn);
        CurrentRound = IsCombatActive ? Math.Max(1, session.Combatants.Count > 0 ? 1 : 0) : 1;

        LoadPartyCharacters();
    }

    private void LoadPartyCharacters()
    {
        PartyCharacters.Clear();
        var allCharacters = _characterService.LoadAllCharacters();
        foreach (var id in _campaign.CharacterIds)
        {
            var character = allCharacters.FirstOrDefault(c => c.Id == id);
            if (character != null)
                PartyCharacters.Add(character);
        }
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

    [RelayCommand]
    private void AddCombatant()
    {
        if (string.IsNullOrWhiteSpace(NewCombatantName)) return;
        
        var combatant = new Combatant
        {
            Name = NewCombatantName.Trim(),
            Initiative = NewCombatantInitiative,
            MaxHp = NewCombatantHp,
            CurrentHp = NewCombatantHp,
            IsEnemy = NewCombatantIsEnemy,
            IsActiveTurn = false
        };

        InsertCombatantSorted(combatant);
        
        NewCombatantName = string.Empty;
        NewCombatantInitiative = 0;
        NewCombatantHp = 10;
        
        Save();
    }

    [RelayCommand]
    private void AddPartyMemberToCombat(PlayerCharacter? character)
    {
        if (character == null) return;

        if (CombatantsList.Any(c => c.Name == character.CharacterName && !c.IsEnemy))
            return;

        var combatant = new Combatant
        {
            Name = character.CharacterName,
            Initiative = 0,
            MaxHp = character.MaxHp,
            CurrentHp = character.CurrentHp,
            IsEnemy = false,
            IsActiveTurn = false
        };

        InsertCombatantSorted(combatant);
        Save();
    }

    [RelayCommand]
    private void AddAllPartyToCombat()
    {
        foreach (var character in PartyCharacters)
        {
            if (CombatantsList.Any(c => c.Name == character.CharacterName && !c.IsEnemy))
                continue;

            var combatant = new Combatant
            {
                Name = character.CharacterName,
                Initiative = 0,
                MaxHp = character.MaxHp,
                CurrentHp = character.CurrentHp,
                IsEnemy = false,
                IsActiveTurn = false
            };

            Session.Combatants.Add(combatant);
            CombatantsList.Add(combatant);
        }
        Save();
    }

    private void InsertCombatantSorted(Combatant combatant)
    {
        Session.Combatants.Add(combatant);
        
        int insertIndex = 0;
        for (int i = 0; i < CombatantsList.Count; i++)
        {
            if (CombatantsList[i].Initiative >= combatant.Initiative)
                insertIndex = i + 1;
            else
                break;
        }
        CombatantsList.Insert(insertIndex, combatant);
    }

    [RelayCommand]
    private void RemoveCombatant(Combatant? c)
    {
        if (c == null) return;
        CombatantsList.Remove(c);
        Session.Combatants.Remove(c);
        
        if (!CombatantsList.Any())
        {
            IsCombatActive = false;
            CurrentRound = 1;
        }
        Save();
    }

    [RelayCommand]
    private void ClearAllCombatants()
    {
        foreach (var c in CombatantsList)
            c.IsActiveTurn = false;
        
        CombatantsList.Clear();
        Session.Combatants.Clear();
        IsCombatActive = false;
        CurrentRound = 1;
        Save();
    }

    [RelayCommand]
    private void SortInitiative()
    {
        var sorted = CombatantsList.OrderByDescending(x => x.Initiative).ToList();
        CombatantsList.Clear();
        Session.Combatants.Clear();
        foreach (var item in sorted)
        {
            CombatantsList.Add(item);
            Session.Combatants.Add(item);
        }
        Save();
    }

    [RelayCommand]
    private void NextTurn()
    {
        if (!CombatantsList.Any()) return;

        var currentActive = CombatantsList.FirstOrDefault(c => c.IsActiveTurn);
        if (currentActive != null) currentActive.IsActiveTurn = false;

        int nextIndex = 0;
        if (currentActive != null)
        {
            int currentIndex = CombatantsList.IndexOf(currentActive);
            nextIndex = (currentIndex + 1) % CombatantsList.Count;
            
            if (nextIndex == 0)
                CurrentRound++;
        }

        CombatantsList[nextIndex].IsActiveTurn = true;
        IsCombatActive = true;
        Save();
    }

    [RelayCommand]
    private void StartCombat()
    {
        if (!CombatantsList.Any()) return;
        
        SortInitiative();
        
        foreach (var c in CombatantsList)
            c.IsActiveTurn = false;
        
        CombatantsList[0].IsActiveTurn = true;
        IsCombatActive = true;
        CurrentRound = 1;
        Save();
    }

    [RelayCommand]
    private void StopCombat()
    {
        foreach (var c in CombatantsList)
            c.IsActiveTurn = false;
        
        IsCombatActive = false;
        CurrentRound = 1;
        Save();
    }

    private void Save() => _campaignService.SaveSession(_campaign.Id, Session);

    [RelayCommand]
    private void Back()
    {
        Save();
        var vm = ActivatorUtilities.CreateInstance<CampaignDetailViewModel>(_serviceProvider, _campaign);
        _navigationService.NavigateTo(vm);
    }
}