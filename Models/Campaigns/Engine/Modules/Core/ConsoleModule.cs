using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DungeonApp.Models.Campaigns.Engine.Commands;
using DungeonApp.Models.Campaigns.Engine.Events;

namespace DungeonApp.Models.Campaigns.Engine.Modules.Core;

/// <summary>
/// Serce komunikacyjne Mistrza Gry. Moduł zbierający wszystkie notyfikacje,
/// logujący propozycje i rozgłaszający komendy DM-a na szynę EventBus.
/// </summary>
/// <remarks>
/// ConsoleModule NIE parsuje komend. Jego jedyna odpowiedzialność to:
/// 1. Zbieranie NotificationEvent i ProposalEvent do widocznego Feeda.
/// 2. Przyjęcie surowego tekstu od DM-a i wyemitowanie go jako ConsoleCommandEvent
///    — dzięki temu każdy moduł (Timekeeper, Dice, itp.) może sam zareagować
///    na swój prefix bez ingerencji w kod konsoli.
/// 3. Obsługa systemu autouzupełniania (Brigadier-style) przez AutocompleteEngine.
/// </remarks>
public partial class ConsoleModule : CampaignModuleBase,
    IRecipient<NotificationEvent>,
    IRecipient<ProposalEvent>,
    IRecipient<RegisterCommandEvent>
{
    public override string ModuleId => "Core.Console";

    // ─── Feed ────────────────────────────────────────────────────────────────

    /// <summary>Historia zdarzeń wyświetlana w Konsoli (Notyfikacje i Propozycje).</summary>
    public ObservableCollection<CampaignEventBase> Feed { get; } = new();

    // ─── Autocomplete Engine (Brigadier-style) ───────────────────────────────

    /// <summary>
    /// Silnik autouzupełniania — rejestruje drzewa komend i generuje kontekstowe podpowiedzi.
    /// Bezstanowy: dostaje tekst + pozycję kursora, zwraca SuggestionResult.
    /// </summary>
    private readonly AutocompleteEngine _autocompleteEngine = new();

    /// <summary>
    /// Lista aktualnie wpisanych tokenów wraz z ich walidacją (kolorami),
    /// bindowana w XAML do ItemsControl nakładanego pod (lub na) TextBox.
    /// </summary>
    [ObservableProperty]
    private System.Collections.Generic.IEnumerable<ParsedToken> _parsedTokens = Array.Empty<ParsedToken>();

    /// <summary>
    /// Aktualne podpowiedzi — lista stringów bindowana do ListBoxa w Popup.
    /// Może zawierać: "morning", "noon", "<<czas>>" (hint dla Number), itp.
    /// </summary>
    public ObservableCollection<string> FilteredCommands { get; } = new();

    /// <summary>
    /// Pozycja (indeks znaku) w ConsoleInputText, od której zaczyna się bieżący token.
    /// Odpowiednik getStart() z Brigadier SuggestionsBuilder.
    /// Używana przez code-behind do podmieniania tylko bieżącego tokenu (nie całego wiersza).
    /// </summary>
    public int CurrentTokenStart { get; private set; }

    [ObservableProperty]
    private bool _isAutocompleteOpen;

    [ObservableProperty]
    private string _consoleInputText = string.Empty;

    [ObservableProperty]
    private string? _selectedCommand;

    [ObservableProperty]
    private string _ghostText = string.Empty;

    private bool _suppressFilterUpdate;

    partial void OnSelectedCommandChanged(string? value)
    {
        UpdateGhostText();
    }

    private void UpdateGhostText()
    {
        var value = SelectedCommand;
        if (value == null || value.StartsWith(CommandNode.HintPrefix) || CurrentTokenStart < 0 || string.IsNullOrEmpty(ConsoleInputText))
        {
            GhostText = string.Empty;
            return;
        }

        var prefix = ConsoleInputText.Length >= CurrentTokenStart
            ? ConsoleInputText[..CurrentTokenStart]
            : ConsoleInputText;

        string newGhost = prefix + value;

        // Inteligentny Ghost Text: Renderuj TYLKO jeśli wpisany tekst w 100% pasuje do początku tego, co chcemy wyświetlić!
        // To eliminuje nakładanie się na siebie liter podczas przewijania strzałkami opcji po wcześniejszym wciśnięciu Tab.
        GhostText = newGhost.StartsWith(ConsoleInputText, StringComparison.OrdinalIgnoreCase) 
            ? newGhost 
            : string.Empty;
    }

    // ─── Logika autouzupełniania ─────────────────────────────────────────────

    partial void OnConsoleInputTextChanged(string value)
    {
        if (_suppressFilterUpdate)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            SelectedCommand = null;
            IsAutocompleteOpen = false;
            FilteredCommands.Clear();
            ParsedTokens = Array.Empty<ParsedToken>();
            GhostText = string.Empty;
            return;
        }

        var parseResult = _autocompleteEngine.Parse(value, value.Length);
        
        // Aktualizacja listy podświetlanych tokenów (Syntax Highlighting)
        ParsedTokens = parseResult.Tokens;

        if (!value.StartsWith("/"))
        {
            SelectedCommand = null;
            IsAutocompleteOpen = false;
            FilteredCommands.Clear();
            GhostText = string.Empty;
            return;
        }

        var result = parseResult.Suggestions;

        CurrentTokenStart = result.TokenStartOverride ?? value.Length;
        FilteredCommands.Clear();
        foreach (var suggestion in result.Suggestions)
        {
            FilteredCommands.Add(suggestion);
        }

        if (FilteredCommands.Count > 0)
        {
            // Zmiana może być ignorowana przez ObservableProperty, jeśli tekst jest ten sam,
            // ale my i tak wywołamy UpdateGhostText() na końcu tej metody!
            var first = FilteredCommands[0];
            SelectedCommand = first.StartsWith(CommandNode.HintPrefix) ? null : first;
            IsAutocompleteOpen = true;
        }
        else
        {
            SelectedCommand = null;
            IsAutocompleteOpen = false;
            GhostText = string.Empty;
        }

        UpdateGhostText();
    }

    // ─── Inicjalizacja ───────────────────────────────────────────────────────

    protected override void OnInitialize()
    {
        base.OnInitialize();

        if (Messenger != null)
        {
            Messenger.Register<NotificationEvent>(this);
            Messenger.Register<ProposalEvent>(this);
            Messenger.Register<RegisterCommandEvent>(this);
        }

        System.Diagnostics.Debug.WriteLine("[ConsoleModule] Konsola uruchomiona i nasłuchuje.");
    }

    // ─── Receive ─────────────────────────────────────────────────────────────

    public void Receive(NotificationEvent message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Feed.Add(message));
        System.Diagnostics.Debug.WriteLine($"[ConsoleFeed] [{message.Level}] {message.SenderModuleId}: {message.Message}");
    }

    public void Receive(ProposalEvent message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Feed.Add(message));
        System.Diagnostics.Debug.WriteLine($"[ConsoleFeed] [PROPOSAL] {message.SenderModuleId}: {message.Description}");
    }

    public void Receive(RegisterCommandEvent message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _autocompleteEngine.RegisterCommand(message.RootNode, message.SenderModuleId);
        });
    }

    // ─── Komendy UI ─────────────────────────────────────────────────────────

    [RelayCommand]
    public void ExecuteConsoleCommand()
    {
        var input = ConsoleInputText;
        ConsoleInputText = string.Empty;

        System.Diagnostics.Debug.WriteLine($"[ConsoleInput] {input}");

        if (string.IsNullOrWhiteSpace(input)) return;

        if (input.StartsWith("/"))
        {
            var result = _autocompleteEngine.Execute(input);
            if (!result.IsSilent)
            {
                Publish(new NotificationEvent(result.Message, result.Level) { SenderModuleId = result.SenderId ?? "Core.Console" });
            }
        }
        else
        {
            Publish(new NotificationEvent(input, "DM") { SenderModuleId = "DM" });
        }
    }

    [RelayCommand]
    private void AcceptProposal(ProposalEvent? proposal)
    {
        if (proposal == null) return;
        proposal.AcceptAction?.Invoke();
        Feed.Remove(proposal);
    }

    [RelayCommand]
    private void RejectProposal(ProposalEvent? proposal)
    {
        if (proposal == null) return;
        proposal.RejectAction?.Invoke();
        Feed.Remove(proposal);
    }

    // ─── Autouzupełnianie (wywoływane z code-behind) ─────────────────────────

    public void ApplyAutocomplete(string suggestion)
    {
        if (suggestion.StartsWith(CommandNode.HintPrefix)) return;

        var prefix = ConsoleInputText.Length >= CurrentTokenStart
            ? ConsoleInputText[..CurrentTokenStart]
            : ConsoleInputText;

        _suppressFilterUpdate = true;
        var newText = prefix + suggestion;
        ConsoleInputText = newText;
        
        // Manualnie wymuś parsowanie, by X-Ray pokolorował nowy tekst bez niszczenia stanu podpowiedzi
        var parseResult = _autocompleteEngine.Parse(newText, newText.Length);
        ParsedTokens = parseResult.Tokens;

        _suppressFilterUpdate = false;
        
        UpdateGhostText();
    }
}
