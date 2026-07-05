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

    public CharacterDetailViewModel(
        PlayerCharacter character,
        INavigationService navigationService,
        ICharacterService characterService)
    {
        Character = character;
        _navigationService = navigationService;
        _characterService = characterService;
    }

    [RelayCommand]
    private void AddEquipmentItem()
    {
        Character.EquipmentItems.Add(new EquipmentItem { Name = "Nowy przedmiot", Quantity = 1 });
    }

    [RelayCommand]
    private void RemoveEquipmentItem(EquipmentItem? item)
    {
        if (item != null)
        {
            Character.EquipmentItems.Remove(item);
        }
    }

    [RelayCommand]
    private void Save()
    {
        _characterService.SaveCharacter(Character);
        if (App.Current?.Services?.GetService(typeof(MainWindowViewModel)) is MainWindowViewModel mainVm)
        {
            mainVm.RefreshCharactersList(Character.Id);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (App.Current?.Services?.GetService(typeof(MainWindowViewModel)) is MainWindowViewModel mainVm)
        {
            mainVm.RefreshCharactersList(null);
        }
    }

    [RelayCommand]
    private void Delete()
    {
        _characterService.DeleteCharacter(Character.Id);
        if (App.Current?.Services?.GetService(typeof(MainWindowViewModel)) is MainWindowViewModel mainVm)
        {
            mainVm.RefreshCharactersList(null);
        }
    }
}
