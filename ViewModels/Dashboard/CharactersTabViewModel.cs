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

    public CharactersTabViewModel(ICharacterService characterService, INavigationService navigationService)
    {
        _characterService = characterService;
        NavigationService = navigationService;
        _ = RefreshCharactersList();
    }

    public async Task RefreshCharactersList(string? keepSelectedId = null)
    {
        Characters.Clear();
        foreach (var character in _characterService.LoadAllCharacters())
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
        NavigationService.NavigateTo(vm);
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
