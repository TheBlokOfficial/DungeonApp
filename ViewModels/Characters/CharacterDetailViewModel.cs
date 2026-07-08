using System;
using System.Linq;
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

        Character.EquipmentItems.CollectionChanged += (s, e) => UpdatePagination();
    }

    [ObservableProperty]
    private bool _isEditingIdentity;

    [RelayCommand]
    private void ToggleEditIdentity() => IsEditingIdentity = !IsEditingIdentity;



    [ObservableProperty]
    private bool _isEditingCombat;

    [RelayCommand]
    private void ToggleEditCombat() => IsEditingCombat = !IsEditingCombat;

    // --- Pagination ---
    private const int ItemsPerPage = 8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaginatedEquipment))]
    [NotifyPropertyChangedFor(nameof(TotalEquipmentPages))]
    private int _currentEquipmentPage = 1;

    public int TotalEquipmentPages => Math.Max(1, (int)Math.Ceiling(Character.EquipmentItems.Count / (double)ItemsPerPage));

    public System.Collections.Generic.IEnumerable<EquipmentItem> PaginatedEquipment => 
        Character.EquipmentItems.Skip((CurrentEquipmentPage - 1) * ItemsPerPage).Take(ItemsPerPage);

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentEquipmentPage < TotalEquipmentPages) CurrentEquipmentPage++;
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentEquipmentPage > 1) CurrentEquipmentPage--;
    }

    private void UpdatePagination()
    {
        OnPropertyChanged(nameof(TotalEquipmentPages));
        OnPropertyChanged(nameof(PaginatedEquipment));
        if (CurrentEquipmentPage > TotalEquipmentPages)
            CurrentEquipmentPage = TotalEquipmentPages;
    }

    [RelayCommand]
    private void AddEquipmentItem()
    {
        Character.EquipmentItems.Add(new EquipmentItem { Name = "Nowy przedmiot", Quantity = 1 });
        CurrentEquipmentPage = TotalEquipmentPages;
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
        if (App.Current?.Services?.GetService(typeof(DungeonApp.ViewModels.Dashboard.CharactersTabViewModel)) is DungeonApp.ViewModels.Dashboard.CharactersTabViewModel charsVm)
        {
            _ = charsVm.RefreshCharactersList(Character.Id);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (App.Current?.Services?.GetService(typeof(DungeonApp.ViewModels.Dashboard.CharactersTabViewModel)) is DungeonApp.ViewModels.Dashboard.CharactersTabViewModel charsVm)
        {
            _ = charsVm.RefreshCharactersList(null);
        }
    }

    [RelayCommand]
    private void Delete()
    {
        _characterService.DeleteCharacter(Character.Id);
        if (App.Current?.Services?.GetService(typeof(DungeonApp.ViewModels.Dashboard.CharactersTabViewModel)) is DungeonApp.ViewModels.Dashboard.CharactersTabViewModel charsVm)
        {
            _ = charsVm.RefreshCharactersList(null);
        }
    }
}
