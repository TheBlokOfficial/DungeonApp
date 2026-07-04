using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;

namespace DungeonApp.ViewModels;

public partial class CreateCharacterViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ICharacterService _characterService;

    [ObservableProperty]
    private string _characterName = string.Empty;

    [ObservableProperty]
    private string _playerName = string.Empty;

    [ObservableProperty]
    private string _race = string.Empty;

    [ObservableProperty]
    private string _classAndLevel = string.Empty;

    public CreateCharacterViewModel(
        INavigationService navigationService,
        ICharacterService characterService)
    {
        _navigationService = navigationService;
        _characterService = characterService;
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.NavigateBack();
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

        if (App.Current?.Services?.GetService(typeof(MainWindowViewModel)) is MainWindowViewModel mainVm)
        {
            mainVm.LoadCharactersFromDisk();
        }

        _navigationService.NavigateBack();
    }
}