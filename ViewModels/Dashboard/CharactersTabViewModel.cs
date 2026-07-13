using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DungeonApp.ViewModels.Dashboard;

public partial class CharactersTabViewModel : ViewModelBase
{
    private readonly ICharacterService _characterService;
    public INavigationService NavigationService { get; }

    public ObservableCollection<PlayerCharacter> Characters { get; } = new();

    private CharacterDetailViewModel? _selectedCharacterDetail;
    public CharacterDetailViewModel? SelectedCharacterDetail
    {
        get => _selectedCharacterDetail;
        set => SetProperty(ref _selectedCharacterDetail, value);
    }

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isDataLoaded;

    public CharactersTabViewModel(ICharacterService characterService, INavigationService navigationService)
    {
        _characterService = characterService;
        NavigationService = navigationService;

        // Singleton — ładujemy dane dokładnie raz, od razu w tle.
        _ = InitialLoadAsync();
    }

    /// <summary>
    /// Jednorazowe ładowanie danych przy starcie Singletona z pełną obsługą błędów.
    /// </summary>
    private async Task InitialLoadAsync()
    {
        try
        {
            IsLoading = true;
            await RefreshCharactersList();
            IsDataLoaded = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CharactersTab] Błąd ładowania postaci: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshCharactersList(string? keepSelectedId = null)
    {
        var characters = await Task.Run(() => _characterService.LoadAllCharacters());
        
        Characters.Clear();
        foreach (var character in characters)
        {
            Characters.Add(character);
        }

        if (keepSelectedId != null)
        {
            foreach (var ch in Characters)
            {
                if (ch.Id == keepSelectedId)
                {
                    SelectCharacter(ch);
                    return;
                }
            }
        }
        SelectedCharacterDetail = null;
    }

    [RelayCommand]
    private void RefreshCharacters() => _ = RefreshCharactersList();

    [RelayCommand]
    private void CreateNewCharacter()
    {
        var vm = App.Current!.Services!.GetRequiredService<CreateCharacterViewModel>();
        NavigationService.ShowOverlay(vm);
    }

    [RelayCommand]
    private void SelectCharacter(PlayerCharacter character)
    {
        SelectedCharacterDetail = ActivatorUtilities.CreateInstance<CharacterDetailViewModel>(
            App.Current!.Services!, character);
    }

    [RelayCommand]
    private async Task DeleteCharacter(PlayerCharacter character)
    {
        _characterService.DeleteCharacter(character.Id);
        await RefreshCharactersList();
    }
}
