using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.Models;

/// <summary>
/// Model Kampanii – plik zapisu gry (odpowiednik "save game" w grach komputerowych).
/// </summary>
/// <remarks>
/// Kampania jest bytem AKTYWNYM – przechowuje stan konkretnej rozgrywki
/// (postacie, notatki, postęp fabularny). Nie zawiera w sobie żadnych
/// definicji przedmiotów ani potworów – te pobiera z pasywnych Paczek (Content Packs)
/// poprzez listę ActivePackIds.
///
/// Architektura inspirowana zapisami z gier komputerowych:
/// - Paczka (Content Pack) = zainstalowana gra na dysku (zawiera zasady, tekst, zasoby).
/// - Kampania = plik .sav (zawiera wyłącznie stan postępu gracza).
///
/// Jedna Kampania może mieć podpięte wiele paczek jednocześnie
/// (np. "D&D Core" + "Sci-Fi Addon" + "Curse of Strahd"),
/// a ten sam scenariusz fabularny może być rozgrywany równolegle
/// w wielu różnych Kampaniach (np. z dwoma grupami znajomych).
/// </remarks>
public partial class Campaign : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _system = "D&D 5e";

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private DateTime _lastSession;

    [ObservableProperty]
    private int _sessionsCount;

    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>
    /// Lista identyfikatorów postaci przypisanych do tej kampanii.
    /// </summary>
    [ObservableProperty]
    private List<string> _characterIds = new();

    /// <summary>
    /// Lista identyfikatorów aktywnych paczek zawartości (Content Packs).
    /// </summary>
    /// <remarks>
    /// Kontroluje, z jakich paczek gracze mogą wyszukiwać i dodawać
    /// przedmioty do plecaków oraz jakie potwory DM może wrzucać do Combat Trackera.
    /// Mistrz Gry ustawia tę listę w opcjach Kampanii.
    ///
    /// Przykład: ["dnd_core", "scifi_addon", "curse_of_strahd"]
    /// </remarks>
    [ObservableProperty]
    private List<string> _activePackIds = new();

    /// <summary>
    /// Słownik postępu fabularnego ze wszystkich aktywnych scenariuszy.
    /// </summary>
    /// <remarks>
    /// Klucze mają format "[AdventureId]:[CheckpointKey]"
    /// (np. "curse_of_strahd:ch1_village_arrived" -> true).
    ///
    /// Dzięki temu jedna Kampania może śledzić postępy z wielu
    /// różnych scenariuszy jednocześnie (np. łącząc moduły fabularne
    /// z kilku paczek w jedną wielką meta-grę).
    ///
    /// Wartość 'true' oznacza, że DM odhaczył dany checkpoint.
    /// Brak klucza w słowniku oznacza, że checkpoint nie został jeszcze osiągnięty.
    /// </remarks>
    [ObservableProperty]
    private Dictionary<string, bool> _adventureState = new();
}
