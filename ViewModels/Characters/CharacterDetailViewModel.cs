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
    private readonly IContentRegistry _contentRegistry;

    public PlayerCharacter Character { get; }

    public CharacterDetailViewModel(
        PlayerCharacter character,
        INavigationService navigationService,
        ICharacterService characterService,
        IContentRegistry contentRegistry)
    {
        Character = character;
        _navigationService = navigationService;
        _characterService = characterService;
        _contentRegistry = contentRegistry;

        Character.EquipmentItems.CollectionChanged += (s, e) => UpdatePagination();
        LoadRegistryItems();
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
        Character.EquipmentItems.Add(new EquipmentItem { CustomName = "Nowy przedmiot", Quantity = 1 });
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

    // --- ZAAWANSOWANE WYSZUKIWANIE Z REJESTRU ---

    private System.Collections.Generic.IReadOnlyList<DungeonApp.ViewModels.Dashboard.ItemEntry> _allRegistryItems = new System.Collections.Generic.List<DungeonApp.ViewModels.Dashboard.ItemEntry>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredRegistryItems))]
    private string _registrySearchQuery = string.Empty;

    public System.Collections.Generic.IEnumerable<DungeonApp.ViewModels.Dashboard.ItemEntry> FilteredRegistryItems
    {
        get
        {
            if (string.IsNullOrWhiteSpace(RegistrySearchQuery))
                return _allRegistryItems;

            var queryParts = RegistrySearchQuery.ToLowerInvariant().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            
            return _allRegistryItems.Where(item => 
            {
                bool matchesAll = true;
                foreach (var part in queryParts)
                {
                    if (part.StartsWith("@") && part.Length > 1)
                    {
                        var packQuery = part.Substring(1);
                        if (!item.PackName.ToLowerInvariant().Contains(packQuery)) matchesAll = false;
                    }
                    else if (part.StartsWith("#") && part.Length > 1)
                    {
                        var tagQuery = part.Substring(1);
                        if (!item.Tags.Any(t => t.ToLowerInvariant().Contains(tagQuery))) matchesAll = false;
                    }
                    else
                    {
                        if (!item.Name.ToLowerInvariant().Contains(part)) matchesAll = false;
                    }
                }
                return matchesAll;
            });
        }
    }

    private void LoadRegistryItems()
    {
        var ts = App.Current?.Services?.GetService(typeof(DungeonApp.Services.ITranslationService)) as DungeonApp.Services.ITranslationService;
        var rawItems = _contentRegistry.GetAllItems();
        _allRegistryItems = rawItems.Select(r => new DungeonApp.ViewModels.Dashboard.ItemEntry
        {
            FullId = r.FullId,
            PackName = ts?.Translate(r.PackName) ?? r.PackName,
            Name = ts?.Translate(r.Item.Name) ?? r.Item.Name,
            Description = ts?.Translate(r.Item.Description) ?? r.Item.Description,
            Type = ts?.Translate(r.Item.Type) ?? r.Item.Type,
            Rarity = ts?.Translate(r.Item.Rarity) ?? r.Item.Rarity,
            Weight = r.Item.Weight,
            Tags = r.Item.Tags.Select(t => ts?.Translate(t) ?? t).ToList(),
            Components = r.Item.Components
        }).ToList();
        OnPropertyChanged(nameof(FilteredRegistryItems));
    }

    [ObservableProperty]
    private bool _isRegistryFlyoutOpen;

    [RelayCommand]
    private void AddRegistryItem(string fullTemplateId)
    {
        if (!string.IsNullOrEmpty(fullTemplateId))
        {
            Character.EquipmentItems.Add(new EquipmentItem { TemplateId = fullTemplateId, Quantity = 1 });
            CurrentEquipmentPage = TotalEquipmentPages;
            RegistrySearchQuery = string.Empty; // Reset wyszukiwarki po dodaniu
            IsRegistryFlyoutOpen = false; // Zamknięcie Flyouta
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
