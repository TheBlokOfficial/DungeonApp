using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
/// </remarks>
public partial class ConsoleModule : CampaignModuleBase, 
    IRecipient<NotificationEvent>, 
    IRecipient<ProposalEvent>
{
    public override string ModuleId => "Core.Console";

    // Bindowane do UI Konsoli
    public ObservableCollection<CampaignEventBase> Feed { get; } = new();

    [ObservableProperty]
    private string _consoleInputText = string.Empty;

    protected override void OnInitialize()
    {
        base.OnInitialize();

        if (Messenger != null)
        {
            Messenger.Register<NotificationEvent>(this);
            Messenger.Register<ProposalEvent>(this);
        }
        
        System.Diagnostics.Debug.WriteLine("[ConsoleModule] Konsola uruchomiona i nasłuchuje.");
    }

    /// <summary>
    /// Obsługa cichych notyfikacji z modułów (np. "Upłynęło 8 godzin.").
    /// </summary>
    public void Receive(NotificationEvent message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Feed.Add(message));
        System.Diagnostics.Debug.WriteLine($"[ConsoleFeed] [{message.Level}] {message.SenderModuleId}: {message.Message}");
    }

    /// <summary>
    /// Obsługa propozycji wymagających akceptacji lub odrzucenia przez DM-a.
    /// </summary>
    public void Receive(ProposalEvent message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Feed.Add(message));
        System.Diagnostics.Debug.WriteLine($"[ConsoleFeed] [PROPOSAL] {message.SenderModuleId}: {message.Description}");
    }

    /// <summary>
    /// Przyjmuje tekst wpisany przez DM-a i rozgłasza go jako ConsoleCommandEvent.
    /// Komendy zaczynające się od '/' są traktowane jako systemowe i trafiają na szynę.
    /// Pozostałe są logowane jako zwykłe notatki DM-a.
    /// </summary>
    [RelayCommand]
    public void ExecuteConsoleCommand()
    {
        var input = ConsoleInputText;
        ConsoleInputText = string.Empty;

        System.Diagnostics.Debug.WriteLine($"[ConsoleInput] {input}");

        if (string.IsNullOrWhiteSpace(input)) return;

        if (input.StartsWith("/"))
        {
            // Rozgłoszenie komendy — niech właściwy moduł ją zinterpretuje
            Publish(new ConsoleCommandEvent(input) { SenderModuleId = ModuleId });
        }
        else
        {
            // Zwykła notatka DM-a — zaloguj ją do Feeda jako info
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
}
