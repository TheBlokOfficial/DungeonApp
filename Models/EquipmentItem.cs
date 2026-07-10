using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.Models;

/// <summary>
/// Pojedynczy przedmiot w ekwipunku postaci gracza.
/// </summary>
/// <remarks>
/// System By-Reference (inspirowany architekturą Resource Packów z Minecrafta):
///
/// Przedmiot może być jednym z dwóch typów:
///
/// 1. REFERENCYJNY (z paczki) – posiada wypełnione TemplateId (np. "dnd_core:longsword").
///    Wszystkie statystyki (nazwa, obrażenia, tagi) są pobierane na żywo
///    z zainstalowanej paczki poprzez ContentRegistry.ResolveItem().
///    Jeśli paczka zostanie usunięta z dysku, UI wyświetli Fallback
///    z ikoną ostrzegawczą ⚠️ i surowym ID, ale nie zniszczy wpisu w plecaku.
///
/// 2. CUSTOMOWY (ręczny) – posiada puste TemplateId i wypełnione CustomName.
///    Jest to przedmiot stworzony przez gracza od zera (np. "Magiczny Kamień Szczęścia").
///    Jego statystyki nie zależą od żadnej paczki.
///
/// Dzięki temu plik zapisu postaci jest "lekki" – plecak z 50 przedmiotami
/// to tylko 50 krótkich wpisów z TemplateId, a nie 50 zduplikowanych
/// bloków statystyk, które mogłyby się rozjechać z aktualizacjami paczek.
/// </remarks>
public partial class EquipmentItem : ObservableObject
{
    /// <summary>
    /// Pełny identyfikator szablonu z paczki (np. "dnd_core:longsword").
    /// </summary>
    /// <remarks>
    /// Jeśli jest wypełniony, przedmiot jest REFERENCYJNY – jego nazwa i statystyki
    /// pobierane są z ContentRegistry w locie.
    /// Jeśli jest pusty (null lub ""), przedmiot jest CUSTOMOWY – używa CustomName.
    /// </remarks>
    [ObservableProperty]
    private string? _templateId;

    /// <summary>
    /// Nazwa przedmiotu wpisana ręcznie przez gracza (tylko dla przedmiotów customowych).
    /// </summary>
    /// <remarks>
    /// Używana wyłącznie gdy TemplateId jest puste.
    /// Dla przedmiotów referencyjnych nazwa pochodzi z szablonu w paczce.
    /// </remarks>
    [ObservableProperty]
    private string _customName = string.Empty;

    /// <summary>
    /// Ilość sztuk danego przedmiotu w ekwipunku.
    /// </summary>
    [ObservableProperty]
    private int _quantity = 1;

    /// <summary>
    /// Określa, czy przedmiot jest referencyjny (pochodzi z paczki).
    /// </summary>
    public bool IsFromPack => !string.IsNullOrEmpty(TemplateId);

    /// <summary>
    /// Pobiera nazwę przedmiotu (lub z ContentRegistry, lub CustomName).
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (!IsFromPack)
                return string.IsNullOrEmpty(CustomName) ? "Nowy przedmiot" : CustomName;

            var app = App.Current as App;
            var registry = app?.Services?.GetService(typeof(DungeonApp.Services.IContentRegistry)) as DungeonApp.Services.IContentRegistry;
            var ts = app?.Services?.GetService(typeof(DungeonApp.Services.ITranslationService)) as DungeonApp.Services.ITranslationService;
            
            if (registry != null)
            {
                var template = registry.ResolveItem(TemplateId!);
                if (template != null)
                    return ts?.Translate(template.Name) ?? template.Name;
            }
            
            return $"Brakujący: {TemplateId}";
        }
    }

    /// <summary>
    /// Zwraca sformatowaną nazwę paczki (np. "DungeonApp Core /") dla UI.
    /// Zwraca pusty ciąg, jeśli przedmiot jest customowy.
    /// </summary>
    public string PackNameDisplay
    {
        get
        {
            if (!IsFromPack) return string.IsNullOrEmpty(CustomName) ? "Nowy przedmiot" : CustomName;
            
            var app = App.Current as App;
            var registry = app?.Services?.GetService(typeof(DungeonApp.Services.IContentRegistry)) as DungeonApp.Services.IContentRegistry;
            var ts = app?.Services?.GetService(typeof(DungeonApp.Services.ITranslationService)) as DungeonApp.Services.ITranslationService;

            if (registry != null)
            {
                var packId = TemplateId!.Split(':')[0];
                var pack = registry.GetPack(packId);
                var packName = ts?.Translate(pack?.Name ?? packId) ?? pack?.Name ?? packId;
                return $"{packName} /";
            }
            return "⚠️ /";
        }
    }
}
