using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;

namespace DungeonApp.ViewModels;

public partial class CharacterDetailViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ICharacterService _characterService;

    public PlayerCharacter Character { get; }

    public string StrMod => GetModifierString(Character.Strength);
    public string DexMod => GetModifierString(Character.Dexterity);
    public string ConMod => GetModifierString(Character.Constitution);
    public string IntMod => GetModifierString(Character.Intelligence);
    public string WisMod => GetModifierString(Character.Wisdom);
    public string ChaMod => GetModifierString(Character.Charisma);

    public CharacterDetailViewModel(
        PlayerCharacter character,
        INavigationService navigationService,
        ICharacterService characterService)
    {
        Character = character;
        _navigationService = navigationService;
        _characterService = characterService;

        Character.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Character.Strength)) OnPropertyChanged(nameof(StrMod));
            if (e.PropertyName == nameof(Character.Dexterity)) OnPropertyChanged(nameof(DexMod));
            if (e.PropertyName == nameof(Character.Constitution)) OnPropertyChanged(nameof(ConMod));
            if (e.PropertyName == nameof(Character.Intelligence)) OnPropertyChanged(nameof(IntMod));
            if (e.PropertyName == nameof(Character.Wisdom)) OnPropertyChanged(nameof(WisMod));
            if (e.PropertyName == nameof(Character.Charisma)) OnPropertyChanged(nameof(ChaMod));
        };
    }

    private string GetModifierString(int score)
    {
        int mod = (score - 10) / 2;
        if (score < 10 && (score - 10) % 2 != 0) mod -= 1;
        return mod >= 0 ? $"+{mod}" : mod.ToString();
    }

    [RelayCommand]
    private void Save()
    {
        _characterService.SaveCharacter(Character);
        _navigationService.NavigateBack();
    }

    [RelayCommand]
    private void Cancel()
    {
        if (App.Current?.Services?.GetService(typeof(MainWindowViewModel)) is MainWindowViewModel mainVm)
        {
            mainVm.LoadCharactersFromDisk();
        }
        _navigationService.NavigateBack();
    }
}
