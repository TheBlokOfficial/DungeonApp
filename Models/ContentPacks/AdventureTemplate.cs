using System.Collections.Generic;

namespace DungeonApp.Models.ContentPacks;

/// <summary>
/// Szablon modułu fabularnego (przygody / scenariusza) zdefiniowany w Paczce Zawartości.
/// </summary>
/// <remarks>
/// Jest to byt PASYWNY i SZTYWNY – DM nie może edytować treści scenariusza.
/// Może jedynie interaktować z zaprojektowanymi elementami interaktywnymi
/// (np. odznaczać checkboxy zadań, które są zapisywane w Campaign.AdventureState).
///
/// Dzięki temu jeden scenariusz może być rozgrywany równolegle
/// w wielu różnych Kampaniach (np. z dwoma różnymi grupami graczy),
/// a każda Kampania zapisuje wyłącznie własny stan postępu (Dictionary),
/// nie duplikując tekstu fabularnego.
/// </remarks>
public class AdventureTemplate
{
    /// <summary>
    /// Unikalny identyfikator przygody w obrębie paczki (np. "curse_of_strahd").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Wyświetlana nazwa przygody (np. "Klątwa Strahda").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Opis przygody widoczny przed jej rozpoczęciem.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Lista rozdziałów / sekcji fabularnych przygody.
    /// </summary>
    /// <remarks>
    /// Każdy rozdział zawiera tytuł, treść markdown do odczytania graczom
    /// oraz listę interaktywnych punktów kontrolnych (checkpointów).
    /// </remarks>
    public List<AdventureChapter> Chapters { get; set; } = new();
}

/// <summary>
/// Pojedynczy rozdział scenariusza fabularnego.
/// </summary>
public class AdventureChapter
{
    /// <summary>
    /// Tytuł rozdziału (np. "Rozdział 1: Przybycie do Wioski").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Treść rozdziału w formacie Markdown (do odczytania graczom przez DM-a).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Lista interaktywnych punktów kontrolnych (checkpointów) w tym rozdziale.
    /// </summary>
    /// <remarks>
    /// Każdy checkpoint posiada unikalny klucz (np. "ch1_village_arrived"),
    /// który jest zapisywany w słowniku Campaign.AdventureState.
    /// Dzięki temu stan odznaczenia przetrwa restart aplikacji
    /// i jest niezależny od tekstu scenariusza.
    /// </remarks>
    public List<AdventureCheckpoint> Checkpoints { get; set; } = new();
}

/// <summary>
/// Interaktywny punkt kontrolny (checkbox/zadanie) wewnątrz rozdziału.
/// </summary>
public class AdventureCheckpoint
{
    /// <summary>
    /// Unikalny klucz checkpointu (np. "ch1_dragon_slain").
    /// </summary>
    /// <remarks>
    /// Klucz jest używany jako klucz w słowniku Campaign.AdventureState.
    /// Pełny klucz w słowniku ma format "[AdventureId]:[CheckpointKey]",
    /// dzięki czemu jedna Kampania może śledzić postępy z wielu różnych przygód.
    /// </remarks>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Wyświetlany opis zadania (np. "Gracze pokonali smoka").
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
