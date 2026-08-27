using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonApp.Core.Modules.Dice;

/// <summary>
/// One roll and everything it was made of. The individual dice are kept, not just the total,
/// because a result the GM cannot take apart is a result they have to trust rather than read.
/// </summary>
public sealed record DiceRoll(DiceNotation Notation, IReadOnlyList<int> Dice, int Total);

/// <summary>
/// The dice module keeps nothing. A roll is a fact, not state: once it has happened it belongs to
/// the chronicle, and reopening a campaign must not resurrect yesterday's result as if it were
/// still on the table.
/// </summary>
public sealed record DiceState;

/// <summary>
/// Rolls dice and writes down what fell.
/// <para>
/// The first module that produces a consequence of its own rather than bookkeeping what the GM
/// already decided, which is why it is also the first that has to be believable. It records every
/// die separately and the reason the roll was made, so any result in the chronicle can be read back
/// and checked rather than taken on faith.
/// </para>
/// <para>
/// It knows dice and one flat modifier. What a roll <em>means</em> - whether 14 beats the guard,
/// what the modifier is made of - belongs to the ruleset, and deliberately does not live here.
/// </para>
/// </summary>
public sealed class DiceModule : ICampaignModule
{
    public static ModuleId Id { get; } = ModuleId.Create("core.dice");

    /// <summary>
    /// Injected rather than reached for, so a test can roll a known sequence. This is the whole of
    /// the randomness seam: <see cref="Random"/> is already an abstraction and wrapping it in
    /// another would buy nothing.
    /// </summary>
    private readonly Random _random;

    private ModuleContext? _context;

    public DiceModule(Random? random = null) => _random = random ?? Random.Shared;

    /// <summary>Depends on nothing: a roll is not an event in world time, it is a question answered.</summary>
    public ModuleManifest Manifest { get; } = new(Id, "Kości", StateVersion: 1, Requires: []);

    /// <summary>
    /// Rolls and records. Both the notation and the reason are refused before a single die falls, so
    /// a rejected roll leaves nothing in the chronicle for the GM to wonder about.
    /// </summary>
    public DiceRoll Roll(string? notation, string reason)
    {
        if (_context is null)
        {
            throw new InvalidOperationException("The dice were used before their campaign activated them.");
        }

        var parsed = DiceNotation.Parse(notation);

        // The chronicle records what a roll decided, and a roll with no stated question decided
        // nothing anybody can read back.
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new CampaignRuleException("Rzut wymaga podania, o co jest rzucany.");
        }

        var dice = new int[parsed.Count];

        for (var index = 0; index < dice.Length; index++)
        {
            dice[index] = _random.Next(1, parsed.Sides + 1);
        }

        var roll = new DiceRoll(parsed, dice, dice.Sum() + parsed.Modifier);

        _context.Journal.Record(Describe(roll), reason.Trim());

        // Nothing is announced. A roll is an accomplished fact and would be a natural thing to put
        // on the channel, but no module listens for one yet, and an event with no listener is a
        // guess about the future rather than a requirement of the present.

        return roll;
    }

    /// <summary>
    /// Every die, then the modifier, then the total - the chronicle shows the arithmetic rather than
    /// the verdict, which is what makes a result checkable a week later.
    /// </summary>
    public static string Describe(DiceRoll roll)
    {
        var dice = string.Join(" + ", roll.Dice);
        var modifier = roll.Notation.Modifier switch
        {
            0 => string.Empty,
            > 0 => $" + {roll.Notation.Modifier}",
            _ => $" - {Math.Abs(roll.Notation.Modifier)}"
        };

        // A single die with no modifier needs no arithmetic shown - the total is the die.
        return roll.Dice.Count == 1 && roll.Notation.Modifier == 0
            ? $"Rzut {roll.Notation}: {roll.Total}"
            : $"Rzut {roll.Notation}: {dice}{modifier} = {roll.Total}";
    }

    public void OnActivated(ModuleContext context) => _context = context;

    public Type StateType => typeof(DiceState);

    public object CaptureState() => new DiceState();

    public void RestoreState(object state, int version)
    {
        // Nothing to restore, and nothing to migrate. Kept explicit rather than left to a default,
        // so the emptiness reads as a decision instead of an omission.
    }
}
