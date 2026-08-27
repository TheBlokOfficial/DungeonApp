using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DungeonApp.Core.Modules.Party;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels.Party;

/// <summary>One seat at the table, as the GM reads it.</summary>
public sealed class ParticipantViewModel(
    string playerName,
    string? characterName,
    bool isPresent,
    ICommand togglePresence,
    ICommand remove)
{
    public string PlayerName { get; } = playerName;

    /// <summary>Empty rather than null, so the row keeps its shape before a character is assigned.</summary>
    public string CharacterName { get; } = characterName ?? "—";

    public bool IsPresent { get; } = isPresent;

    /// <summary>Reads as the state, not the action: the row shows who is here today.</summary>
    public string PresenceLabel { get; } = isPresent ? "przy stole" : "nieobecny";

    public ICommand TogglePresence { get; } = togglePresence;

    public ICommand Remove { get; } = remove;
}

/// <summary>
/// Who plays this campaign and who turned up today. It seats and unseats; who may sit down and what
/// counts as a duplicate is the module's business, and so is the wording of any refusal.
/// </summary>
public sealed class PartyPanelViewModel : ObservableObject, IDisposable
{
    private readonly CampaignSession _session;
    private readonly PartyModule _party;

    private string _playerName = string.Empty;
    private string _characterName = string.Empty;
    private string? _message;

    public PartyPanelViewModel(CampaignSession session, PartyModule party)
    {
        _session = session;
        _party = party;

        SeatCommand = new AsyncCommand(SeatAsync, () => CanSeat);

        _session.Committed += Refresh;

        Refresh();
    }

    public ObservableCollection<ParticipantViewModel> Participants { get; } = [];

    public AsyncCommand SeatCommand { get; }

    public bool IsEmpty => Participants.Count == 0;

    public string PlayerName
    {
        get => _playerName;
        set
        {
            if (SetField(ref _playerName, value))
            {
                RaisePropertyChanged(nameof(CanSeat));
                SeatCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Optional: a player can take a seat before they have decided who they play.</summary>
    public string CharacterName
    {
        get => _characterName;
        set => SetField(ref _characterName, value);
    }

    public bool CanSeat => !string.IsNullOrWhiteSpace(PlayerName);

    public string? Message
    {
        get => _message;
        private set => SetField(ref _message, value);
    }

    public void Dispose() => _session.Committed -= Refresh;

    private async Task SeatAsync()
    {
        var player = PlayerName;
        var character = CharacterName;

        Message = await _session.ExecuteAsync(() => _party.Seat(player, character));

        if (Message is null)
        {
            // Cleared only on success, so a refused seat keeps what the GM typed.
            PlayerName = string.Empty;
            CharacterName = string.Empty;
        }
    }

    private void Refresh()
    {
        Participants.Clear();

        foreach (var participant in _party.Participants)
        {
            var id = participant.Id;
            var present = participant.IsPresent;

            Participants.Add(new ParticipantViewModel(
                participant.PlayerName,
                participant.CharacterName,
                present,
                new AsyncCommand(() => RunAsync(() => _party.SetPresence(id, !present))),
                new AsyncCommand(() => RunAsync(() => _party.Remove(id)))));
        }

        RaisePropertyChanged(nameof(IsEmpty));
    }

    private async Task RunAsync(Action operation) => Message = await _session.ExecuteAsync(operation);
}
