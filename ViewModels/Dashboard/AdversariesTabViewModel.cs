using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models.ContentPacks;
using DungeonApp.Models.ContentPacks.Components;
using DungeonApp.Services;

namespace DungeonApp.ViewModels.Dashboard;

/// <summary>
/// ViewModel zakładki "Przeciwnicy" w panelu Rejestru.
/// </summary>
public partial class AdversariesTabViewModel : RegistryTabViewModelBase
{
    private IReadOnlyList<AdversaryEntry> _allAdversaries = new List<AdversaryEntry>();
    private readonly ISettingsService _settingsService;

    public AdversariesTabViewModel(IContentRegistry contentRegistry, ITranslationService translationService, ISettingsService settingsService)
        : base(contentRegistry, translationService)
    {
        _settingsService = settingsService;
        
        _settingsService.SettingsChanged += () =>
        {
            if (IsDataLoaded)
                _ = ReloadAsync();
        };

        // Singleton — ładujemy dane dokładnie raz, od razu w tle.
        _ = ReloadAsync();
    }

    public string DistanceUnit => _settingsService.LoadSettings().DistanceUnit ?? "ft.";

    public IEnumerable<AdversaryEntry> FilteredAdversaries
    {
        get
        {
            var query = SearchQuery?.ToLowerInvariant() ?? "";
            
            var activePacks = PackFilters.Where(p => p.IsSelected).Select(p => p.Value).ToList();
            var activeTypes = TypeFilters.Where(c => c.IsSelected).Select(c => c.Value).ToList();
            var activeSizes = SizeFilters.Where(c => c.IsSelected).Select(c => c.Value).ToList();
            var activeCr = ChallengeRatingFilters.Where(c => c.IsSelected).Select(c => c.Value).ToList();
            var activeTags = TagFilters.Where(t => t.IsSelected).Select(t => t.Value).ToList();
            var activeFactions = FactionFilters.Where(f => f.IsSelected).Select(f => f.Value).ToList();
            
            return _allAdversaries.Where(a =>
            {
                // 1. Wyszukiwarka tekstowa
                bool matchesSearch = string.IsNullOrWhiteSpace(query) ||
                    a.Name.ToLowerInvariant().Contains(query);
                if (!matchesSearch) return false;

                // 2. Filtry Paczek
                if (activePacks.Count > 0 && !activePacks.Contains(a.PackId))
                    return false;

                // 3. Filtry Typu
                if (activeTypes.Count > 0 && !activeTypes.Contains(a.Type))
                    return false;

                // 4. Filtry Rozmiaru
                if (activeSizes.Count > 0 && !activeSizes.Contains(a.Size))
                    return false;

                // 5. Filtry CR
                if (activeCr.Count > 0 && !activeCr.Contains(a.ChallengeRating))
                    return false;

                // 6. Filtry Tagów
                if (activeTags.Count > 0 && !activeTags.All(tag => a.Tags.Contains(tag)))
                    return false;

                // 7. Filtry Frakcji
                if (activeFactions.Count > 0 && !activeFactions.Contains(a.FactionName))
                    return false;

                return true;
            });
        }
    }

    public int TotalAdversaryCount => _allAdversaries.Count;

    protected override async Task LoadDataAsync()
    {
        ClearFilterSubscriptions();

        var mappedAdversaries = await Task.Run(() => 
        {
            var raw = _contentRegistry.GetAllAdversaries();
            return raw.Select(r =>
            {
                var packId = r.FullId.Split(':')[0];
                var adv = r.Adversary;
                
                var entry = new AdversaryEntry
                {
                    FullId = r.FullId,
                    PackId = packId,
                    PackName = _translationService.Translate(r.PackName),
                    Name = _translationService.Translate(adv.Name),
                    Description = _translationService.Translate(adv.Description),
                    Type = adv.Type,
                    Size = adv.Size,
                    Alignment = adv.Alignment,
                    ChallengeRating = adv.ChallengeRating,
                    Tags = adv.Tags.Select(t => _translationService.Translate(t)).ToList(),
                    Combat = adv.Combat,
                    Speeds = adv.Speeds,
                    Abilities = adv.Abilities,
                    AbilityScoresVM = new DungeonApp.ViewModels.Controls.AbilityScoresViewModel(adv.Abilities),
                    Actions = adv.Actions.Select(a => new ActionDefinition { 
                        Name = _translationService.Translate(a.Name), 
                        Description = _translationService.Translate(a.Description)
                    }).ToList(),

                    TagModels = adv.Tags.Select(t => 
                    {
                        var template = _contentRegistry.ResolveTag(t);
                        return new TagViewModel
                        {
                            Name = _translationService.Translate(t),
                            ColorHex = template?.ColorHex ?? "#1E293B" // fallback BadgeDefaultBackground
                        };
                    }).ToList(),
                    TypeDisplay = string.IsNullOrEmpty(adv.Type) ? "" : _translationService.Translate(adv.Type),
                    SizeDisplay = string.IsNullOrEmpty(adv.Size) ? "" : _translationService.Translate(adv.Size),
                    AlignmentDisplay = string.IsNullOrEmpty(adv.Alignment) ? "" : _translationService.Translate(adv.Alignment),
                    ChallengeRatingDisplay = string.IsNullOrEmpty(adv.ChallengeRating) ? "" : _translationService.Translate(adv.ChallengeRating)
                };
                
                if (!string.IsNullOrEmpty(adv.Faction))
                {
                    var factionId = adv.Faction.Contains(':') ? adv.Faction : $"{packId}:{adv.Faction}";
                    var faction = _contentRegistry.ResolveFaction(factionId);
                    if (faction != null)
                    {
                        entry.FactionName = _translationService.Translate(faction.Name);
                        entry.FactionIcon = faction.Icon;
                        entry.FactionColorHex = faction.ColorHex;
                    }
                }
                
                entry.PropertyBadges.Add(new PropertyBadgeViewModel
                {
                    Icon = "IconHeart",
                    Text = $"{adv.Combat.MaxHp}",
                    TextColor = "#FF4D4D" // AccentDanger
                });

                var speeds = adv.Speeds.Select(s => 
                {
                    string formatted = FormatSpeed(s.Value, DistanceUnit);
                    string translatedType = _translationService.Translate(s.Type);
                    return s.Note != null ? $"{translatedType} {formatted} ({s.Note})" : $"{translatedType} {formatted}";
                });
                entry.SpeedDisplay = adv.Speeds.Count > 0 ? string.Join(", ", speeds) : "0 " + DistanceUnit;

                return entry;
            }).ToList();
        });

        _allAdversaries = mappedAdversaries;

        BuildFilters();

        OnPropertyChanged(nameof(FilteredAdversaries));
        OnPropertyChanged(nameof(TotalAdversaryCount));
    }

    private string FormatSpeed(int speedValue, string unit)
    {
        if (unit == "m.")
        {
            double m = speedValue / 5.0 * 1.5;
            return $"{m:0.##} m.";
        }
        return $"{speedValue} ft.";
    }

    private void BuildFilters()
    {
        PackFilters.Clear();
        TypeFilters.Clear();
        SizeFilters.Clear();
        ChallengeRatingFilters.Clear();
        TagFilters.Clear();
        FactionFilters.Clear();

        var uniquePacks = _allAdversaries.Select(m => new { m.PackId, m.PackName }).Distinct().OrderBy(p => p.PackName);
        foreach (var p in uniquePacks)
        {
            var filter = new FilterItemViewModel(p.PackName, p.PackId);
            filter.PropertyChanged += OnFilterChanged;
            PackFilters.Add(filter);
        }

        var uniqueTypes = _allAdversaries.Select(i => i.Type).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();
        foreach (var c in uniqueTypes)
        {
            var filter = new FilterItemViewModel(_translationService.Translate(c), c);
            filter.PropertyChanged += OnFilterChanged;
            TypeFilters.Add(filter);
        }

        var uniqueSizes = _allAdversaries.Select(i => i.Size).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();
        foreach (var c in uniqueSizes)
        {
            var filter = new FilterItemViewModel(_translationService.Translate(c), c);
            filter.PropertyChanged += OnFilterChanged;
            SizeFilters.Add(filter);
        }

        var uniqueCrs = _allAdversaries.Select(i => i.ChallengeRating).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();
        foreach (var c in uniqueCrs)
        {
            var filter = new FilterItemViewModel(_translationService.Translate(c), c);
            filter.PropertyChanged += OnFilterChanged;
            ChallengeRatingFilters.Add(filter);
        }

        var allTags = _allAdversaries.SelectMany(m => m.Tags).Distinct().OrderBy(t => t).ToList();
        foreach (var tag in allTags)
        {
            var item = new FilterItemViewModel(tag, tag);
            item.PropertyChanged += OnFilterChanged;
            TagFilters.Add(item);
        }

        var uniqueFactions = _allAdversaries.Select(i => i.FactionName).Where(f => !string.IsNullOrEmpty(f)).Distinct().OrderBy(f => f).ToList();
        foreach (var f in uniqueFactions)
        {
            var filter = new FilterItemViewModel(f, f);
            filter.PropertyChanged += OnFilterChanged;
            FactionFilters.Add(filter);
        }
    }

    protected override void OnFiltersChanged()
    {
        OnPropertyChanged(nameof(FilteredAdversaries));
    }
}

public class AdversaryEntry
{
    public string FullId { get; set; } = string.Empty;
    public string PackId { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullDisplayName => $"{PackName} - {Name}";
    public string Description { get; set; } = string.Empty;
    
    public string Type { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Alignment { get; set; } = string.Empty;
    public string ChallengeRating { get; set; } = string.Empty;
    
    public string TypeDisplay { get; set; } = string.Empty;

    public string SizeDisplay { get; set; } = string.Empty;
    public string AlignmentDisplay { get; set; } = string.Empty;
    public string ChallengeRatingDisplay { get; set; } = string.Empty;

    public List<string> Traits { get; set; } = new();
    
    public List<string> Tags { get; set; } = new();
    public List<TagViewModel> TagModels { get; set; } = new();
    
    public string FactionName { get; set; } = string.Empty;
    public string FactionIcon { get; set; } = string.Empty;
    public string FactionColorHex { get; set; } = "Transparent";
    public bool HasFaction => !string.IsNullOrEmpty(FactionName);
    
    public CombatStats Combat { get; set; } = new();
    public List<SpeedComponent> Speeds { get; set; } = new();
    public AbilityScores Abilities { get; set; } = new();
    public List<ActionDefinition> Actions { get; set; } = new();
    public bool HasActions => Actions != null && Actions.Count > 0;

    public string HpDisplay => !string.IsNullOrEmpty(Combat.HitDice) ? $"{Combat.MaxHp} ({Combat.HitDice})" : Combat.MaxHp.ToString();
    public string SpeedDisplay { get; set; } = string.Empty;

    public DungeonApp.ViewModels.Controls.AbilityScoresViewModel AbilityScoresVM { get; set; } = null!;

    public string DisplayPath => $"{PackName} / {Name}";
    public string TagsDisplay => Tags.Count > 0 ? string.Join(" · ", Tags) : string.Empty;
    public bool HasTags => Tags.Count > 0;
    
    public System.Collections.ObjectModel.ObservableCollection<PropertyBadgeViewModel> PropertyBadges { get; set; } = new();
    
    public string IdentityDisplay
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(SizeDisplay)) parts.Add(SizeDisplay);
            if (!string.IsNullOrEmpty(TypeDisplay)) parts.Add(TypeDisplay);
            if (!string.IsNullOrEmpty(AlignmentDisplay)) parts.Add(AlignmentDisplay);
            
            return string.Join(" · ", parts);
        }
    }
}
