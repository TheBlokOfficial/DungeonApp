using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonApp.Core.Modules.Party;

/// <summary>
/// Somebody at the table. The player is the person, the character is who they play - kept apart
/// because a player can change character and the seat is still theirs.
/// <para>
/// Identified by a plain <see cref="Guid"/> rather than a wrapper: identity here belongs to the
/// party module, not to the core, and the scheduler already keeps its entries the same way.
/// </para>
/// </summary>
public sealed record Participant(Guid Id, string PlayerName, string? CharacterName, bool IsPresent);

/// <summary>What the party keeps between sessions. Seating order is the order they were added.</summary>
public sealed record PartyState(IReadOnlyList<Participant> Participants);

/// <summary>
/// Who is playing this campaign, and who turned up today.
/// <para>
/// Deliberately bookkeeping and nothing more: it holds no statistics, no sheet and no character
/// state. Those belong to whatever module eventually models characters, and this one only has to
/// know that a seat exists so the rest of the desk has something to point at.
/// </para>
/// <para>
/// Its records carry no reason, unlike the clock's. Seating a player is not a change to the world
/// that needs grounds - the summary is already the whole story.
/// </para>
/// </summary>
public sealed class PartyModule : ICampaignModule
{
    public static ModuleId Id { get; } = ModuleId.Create("core.party");

    /// <summary>Long enough for a name with a nickname in it, short enough to stay a name.</summary>
    public const int MaxNameLength = 60;

    private readonly List<Participant> _participants = [];

    private ModuleContext? _context;

    /// <summary>Depends on nothing: a roster makes sense in a campaign with no world clock at all.</summary>
    public ModuleManifest Manifest { get; } = new(Id, "Drużyna", StateVersion: 1, Requires: []);

    public IReadOnlyList<Participant> Participants => _participants;

    /// <summary>The ones actually here today, in seating order.</summary>
    public IEnumerable<Participant> Present => _participants.Where(participant => participant.IsPresent);

    public void OnActivated(ModuleContext context) => _context = context;

    /// <summary>
    /// Seats a player, present by default - somebody being written down is somebody who turned up.
    /// Everything that can be refused is refused before the list is touched.
    /// </summary>
    public Guid Seat(string playerName, string? characterName = null)
    {
        var context = Activated();

        var player = Require(playerName, "Uczestnik wymaga imienia gracza.", "Imię gracza");
        var character = Optional(characterName, "Imię postaci");

        // Two identical names at one table cannot be told apart on the desk or in the chronicle.
        if (_participants.Any(participant => Matches(participant.PlayerName, player)))
        {
            throw new CampaignRuleException($"Przy stole siedzi już gracz „{player}”.");
        }

        var participant = new Participant(Guid.NewGuid(), player, character, IsPresent: true);
        _participants.Add(participant);

        context.Journal.Record(character is null
            ? $"Do drużyny dołącza {player}"
            : $"Do drużyny dołącza {player} jako {character}");

        return participant.Id;
    }

    /// <summary>Gives a seated player a character, or takes the one they had away.</summary>
    public void SetCharacter(Guid id, string? characterName)
    {
        var context = Activated();

        var index = IndexOf(id);
        var participant = _participants[index];
        var character = Optional(characterName, "Imię postaci");

        if (string.Equals(participant.CharacterName, character, StringComparison.Ordinal))
        {
            return;
        }

        _participants[index] = participant with { CharacterName = character };

        context.Journal.Record(character is null
            ? $"{participant.PlayerName} nie prowadzi już żadnej postaci"
            : $"{participant.PlayerName} prowadzi teraz {character}");
    }

    /// <summary>
    /// Marks somebody as here or away for today. Setting what is already set writes nothing: the
    /// chronicle is there to explain the campaign, not to record every touch of a checkbox.
    /// </summary>
    public void SetPresence(Guid id, bool isPresent)
    {
        var context = Activated();

        var index = IndexOf(id);
        var participant = _participants[index];

        if (participant.IsPresent == isPresent)
        {
            return;
        }

        _participants[index] = participant with { IsPresent = isPresent };

        context.Journal.Record(isPresent
            ? $"{participant.PlayerName} jest przy stole"
            : $"{participant.PlayerName} jest dziś nieobecny");
    }

    /// <summary>
    /// Removes a seat outright. Absence is what a missed session is for - this is for somebody who
    /// has left the campaign.
    /// </summary>
    public void Remove(Guid id)
    {
        var context = Activated();

        var index = IndexOf(id);
        var participant = _participants[index];

        _participants.RemoveAt(index);

        context.Journal.Record($"{participant.PlayerName} opuszcza drużynę");
    }

    public Type StateType => typeof(PartyState);

    public object CaptureState() => new PartyState([.. _participants]);

    public void RestoreState(object state, int version)
    {
        _participants.Clear();
        _participants.AddRange(((PartyState)state).Participants ?? []);
    }

    private ModuleContext Activated() =>
        _context ?? throw new InvalidOperationException(
            "The party was used before its campaign activated it.");

    /// <summary>
    /// A refusal rather than an argument exception: the identifier reaches here from a desk panel
    /// that may be a moment behind the roster, which is a situation the GM should read, not a crash.
    /// </summary>
    private int IndexOf(Guid id)
    {
        var index = _participants.FindIndex(participant => participant.Id == id);

        return index >= 0
            ? index
            : throw new CampaignRuleException("Tego uczestnika nie ma już w drużynie.");
    }

    private static string Require(string? value, string missingMessage, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CampaignRuleException(missingMessage);
        }

        return Measured(value.Trim(), label);
    }

    private static string? Optional(string? value, string label) =>
        string.IsNullOrWhiteSpace(value) ? null : Measured(value.Trim(), label);

    private static string Measured(string value, string label) =>
        value.Length <= MaxNameLength
            ? value
            : throw new CampaignRuleException(
                $"{label} nie może przekraczać {MaxNameLength} znaków.");

    private static bool Matches(string left, string right) =>
        string.Equals(left, right, StringComparison.CurrentCultureIgnoreCase);
}
