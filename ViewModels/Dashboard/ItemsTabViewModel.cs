using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models.ContentPacks;
using DungeonApp.Models.ContentPacks.Components;
using DungeonApp.Services;

namespace DungeonApp.ViewModels.Dashboard;

/// <summary>
/// ViewModel zakładki "Przedmioty" w panelu Rejestru.
/// </summary>
public partial class ItemsTabViewModel : RegistryTabViewModelBase
{
    private IReadOnlyList<ItemEntry> _allItems = new List<ItemEntry>();
    private readonly ISettingsService _settingsService;

    public ItemsTabViewModel(IContentRegistry contentRegistry, ITranslationService translationService, ISettingsService settingsService)
        : base(contentRegistry, translationService)
    {
        _settingsService = settingsService;
        
        _settingsService.SettingsChanged += () =>
        {
            LoadData();
        };

        LoadData();
    }

    public string WeightUnit => _settingsService.LoadSettings().WeightUnit ?? "kg";

    public IEnumerable<ItemEntry> FilteredItems
    {
        get
        {
            var query = SearchQuery?.ToLowerInvariant() ?? "";
            
            var activePacks = PackFilters.Where(p => p.IsSelected).Select(p => p.Value).ToList();
            var activeTypes = TypeFilters.Where(c => c.IsSelected).Select(c => c.Value).ToList();
            var activeRarities = RarityFilters.Where(c => c.IsSelected).Select(c => c.Value).ToList();
            var activeTags = TagFilters.Where(t => t.IsSelected).Select(t => t.Value).ToList();
            
            return _allItems.Where(i =>
            {
                // 1. Wyszukiwarka tekstowa
                bool matchesSearch = string.IsNullOrWhiteSpace(query) ||
                    i.Name.ToLowerInvariant().Contains(query);
                if (!matchesSearch) return false;

                // 2. Filtry Paczek
                if (activePacks.Count > 0 && !activePacks.Contains(i.PackId))
                    return false;

                // 3. Filtry Typu
                if (activeTypes.Count > 0 && !activeTypes.Contains(i.Type))
                    return false;

                // 4. Filtry Rzadkości
                if (activeRarities.Count > 0 && !activeRarities.Contains(i.Rarity))
                    return false;

                // 5. Filtry Tagów
                if (activeTags.Count > 0 && !activeTags.All(tag => i.Tags.Contains(tag)))
                    return false;

                // 6. Suwak Wagi
                var weightFilter = NumericFilters.FirstOrDefault(nf => nf.PropertyKey == "Weight");
                if (weightFilter != null)
                {
                    if (i.Weight < weightFilter.CurrentMin || i.Weight > weightFilter.CurrentMax)
                        return false;
                }

                return true;
            });
        }
    }

    public int TotalItemCount => _allItems.Count;

    protected override void LoadData()
    {
        ClearFilterSubscriptions();

        var rawItems = _contentRegistry.GetAllItems();
        
        _allItems = rawItems.Select(r =>
        {
            var packId = r.FullId.Split(':')[0];
            var item = r.Item;
            
            var rarityTemplate = _contentRegistry.ResolveRarity(item.Rarity);
            
            var entry = new ItemEntry
            {
                FullId = r.FullId,
                PackId = packId,
                PackName = _translationService.Translate(r.PackName),
                Name = _translationService.Translate(item.Name),
                Description = _translationService.Translate(item.Description),
                Type = item.Type,
                Rarity = item.Rarity,
                Weight = WeightUnit == "kg" ? item.Weight / 2.0 : item.Weight,
                WeightUnit = WeightUnit,
                TypeDisplay = string.IsNullOrEmpty(item.Type) ? "" : _translationService.Translate(item.Type),
                RarityDisplay = string.IsNullOrEmpty(item.Rarity) ? "" : _translationService.Translate(item.Rarity),
                RarityColorHex = rarityTemplate?.ColorHex ?? "#808080",
                RarityHasGlow = rarityTemplate?.HasGlowEffect ?? false,
                Tags = item.Tags.Select(t => _translationService.Translate(t)).ToList(),
                Components = item.Components
            };

            var category = _contentRegistry.ResolveCategory(item.Type);
            var formatTemplates = item.RegistryFormat ?? category?.RegistryFormat;

            if (formatTemplates != null)
            {
                foreach (var template in formatTemplates)
                {
                    var evaluatedText = TemplateEvaluator.Evaluate(template.Format, item.Components);
                    if (!string.IsNullOrEmpty(evaluatedText))
                    {
                        var resolvedIcon = template.Icon;
                        if (!string.IsNullOrEmpty(resolvedIcon) && !resolvedIcon.Contains(':'))
                        {
                            resolvedIcon = $"{packId}:{resolvedIcon}";
                        }

                        entry.PropertyBadges.Add(new PropertyBadgeViewModel
                        {
                            Icon = resolvedIcon,
                            Text = evaluatedText,
                            TextColor = template.TextColor
                        });
                    }
                }
            }

            return entry;
        }).ToList();

        BuildFilters();

        OnPropertyChanged(nameof(FilteredItems));
        OnPropertyChanged(nameof(TotalItemCount));
    }

    private void BuildFilters()
    {
        PackFilters.Clear();
        TypeFilters.Clear();
        RarityFilters.Clear();
        TagFilters.Clear();
        NumericFilters.Clear();

        // Budowanie filtrów paczek
        var uniquePacks = _allItems.Select(i => new { i.PackId, i.PackName }).Distinct().OrderBy(p => p.PackName);
        foreach (var p in uniquePacks)
        {
            var filter = new FilterItemViewModel(p.PackName, p.PackId);
            filter.PropertyChanged += OnFilterChanged;
            PackFilters.Add(filter);
        }

        // Budowanie filtrów Typu
        var uniqueTypes = _allItems.Select(i => i.Type).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();
        foreach (var c in uniqueTypes)
        {
            var filter = new FilterItemViewModel(_translationService.Translate(c), c);
            filter.PropertyChanged += OnFilterChanged;
            TypeFilters.Add(filter);
        }

        // Budowanie filtrów Rzadkości
        var uniqueRarities = _allItems.Select(i => i.Rarity).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();
        foreach (var c in uniqueRarities)
        {
            var filter = new FilterItemViewModel(_translationService.Translate(c), c);
            filter.PropertyChanged += OnFilterChanged;
            RarityFilters.Add(filter);
        }

        // Filtry Tagów
        var allTags = _allItems.SelectMany(i => i.Tags).Distinct().OrderBy(t => t).ToList();
        foreach (var tag in allTags)
        {
            var item = new FilterItemViewModel(tag, tag); // translated during mapping to ItemEntry
            item.PropertyChanged += OnFilterChanged;
            TagFilters.Add(item);
        }

        // Suwak wagi (Tylko jeśli różnica istnieje)
        var minWeight = _allItems.Count > 0 ? _allItems.Min(i => i.Weight) : 0;
        var maxWeight = _allItems.Count > 0 ? _allItems.Max(i => i.Weight) : 0;
        if (minWeight < maxWeight)
        {
            var filter = new NumericFilterViewModel("Weight", _translationService.Translate("core_weight"), minWeight, maxWeight);
            filter.PropertyChanged += OnFilterChanged;
            NumericFilters.Add(filter);
        }
    }

    protected override void OnFiltersChanged()
    {
        OnPropertyChanged(nameof(FilteredItems));
    }
}

/// <summary>
/// Płaski model wyświetlany w liście UI (łączy dane z szablonu przedmiotu i paczki).
/// </summary>
public class ItemEntry
{
    public string FullId { get; set; } = string.Empty;
    public string PackId { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullDisplayName => $"{PackName} - {Name}";
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public double Weight { get; set; } = 0;
    public string WeightUnit { get; set; } = "kg";
    
    public string TypeDisplay { get; set; } = string.Empty;
    public string RarityDisplay { get; set; } = string.Empty;
    public string RarityColorHex { get; set; } = "#808080";
    public bool RarityHasGlow { get; set; } = false;
    
    public Avalonia.Media.IEffect? RarityEffect => RarityHasGlow ? new Avalonia.Media.BlurEffect { Radius = 4 } : null;
    
    public List<string> Tags { get; set; } = new();
    
    public string WeightDisplay => Weight > 0 ? $"{Weight} {WeightUnit}" : string.Empty;

    public DungeonApp.Models.ContentPacks.Components.ItemComponents Components { get; set; } = new();

    public string DisplayPath => $"{PackName} / {Name}";
    public string TagsDisplay => Tags.Count > 0 ? string.Join(" · ", Tags) : string.Empty;
    
    // Dodatkowe pomocnicze flagi dla UI (Czy ma dany klocek)
    public bool HasWeapon => Components?.Weapon != null;
    public bool HasArmor => Components?.Armor != null;
    public bool HasArmorMaxDex => Components?.Armor != null && Components.Armor.MaxDexterityBonus.HasValue;
    public bool HasConsumable => Components?.Consumable != null;
    public bool HasModifiers => Components?.Modifiers != null && Components.Modifiers.Count > 0;
    
    public List<PropertyBadgeViewModel> PropertyBadges { get; set; } = new();
}
