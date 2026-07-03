using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;

namespace DungeonApp.ViewModels;

public partial class CreateCharacterViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly CharacterService _characterService = new();

    [ObservableProperty]
    private string _characterName = string.Empty;

    [ObservableProperty]
    private string _playerName = string.Empty;

    [ObservableProperty]
    private string _race = string.Empty;

    [ObservableProperty]
    private string _classAndLevel = string.Empty;

    public CreateCharacterViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    [RelayCommand]
    private void Cancel()
    {
        _mainViewModel.CurrentView = null;
    }

    [RelayCommand]
    private void Create()
    {
        if (string.IsNullOrWhiteSpace(CharacterName)) return;

        var newCharacter = new PlayerCharacter
        {
            CharacterName = CharacterName.Trim(),
            PlayerName = PlayerName.Trim(),
            Race = Race.Trim(),
            ClassAndLevel = ClassAndLevel.Trim()
        };

        _characterService.SaveCharacter(newCharacter);

        // Odświeżamy listę i zamykamy widok
        _mainViewModel.LoadCharactersFromDisk();
        _mainViewModel.CurrentView = null;
    }
}