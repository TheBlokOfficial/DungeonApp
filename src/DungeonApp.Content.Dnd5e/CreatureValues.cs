using System;
using System.Collections.Generic;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.State;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// A creature entity's live numbers: KP, PW maks. and the six ability scores have a base in the
/// entry (<see cref="CombatAspect"/>); current hit points are tracked against PW maks. The keys are
/// what the campaign file stores and what the creature card asks the live part for.
/// <para>
/// Szybkość is prose in the entry ("9 m, wspinaczka 9 m", "7,5 m"), so it has no base the frame could
/// add a modifier to; it stays text until the entry gives it a number.
/// </para>
/// </summary>
public sealed class CreatureValues : IEntityValueRules
{
    public const string ArmorClassKey = "ac";
    public const string MaxHitPointsKey = "hpMax";
    public const string HitPointsKey = "hp";
    public const string StrKey = "str";
    public const string DexKey = "dex";
    public const string ConKey = "con";
    public const string IntKey = "int";
    public const string WisKey = "wis";
    public const string ChaKey = "cha";

    public static CreatureValues Instance { get; } = new();

    private CreatureValues()
    {
    }

    public IReadOnlyList<TrackedValueRule> TrackedValues { get; } = [new HitPointsRule()];

    public IReadOnlyList<BaseValue> BaseValues(ContentValues values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var combat = values.Read<Creature>().Combat;

        return
        [
            new BaseValue(ArmorClassKey, "KP", combat.Ac),
            new BaseValue(MaxHitPointsKey, "PW maks.", combat.Hp),
            new BaseValue(StrKey, "SIŁ", combat.Str),
            new BaseValue(DexKey, "ZRĘ", combat.Dex),
            new BaseValue(ConKey, "KON", combat.Con),
            new BaseValue(IntKey, "INT", combat.Int),
            new BaseValue(WisKey, "MDR", combat.Wis),
            new BaseValue(ChaKey, "CHA", combat.Cha),
        ];
    }

    /// <summary>
    /// Current hit points: "-" takes away down to 0 and reports what did not fit, "+" heals up to the
    /// maximum, "=" or a bare number sets any value - above the maximum or below 0 too, because a state
    /// against the rules shows instead of being blocked. A "+" on a creature already above its
    /// maximum leaves it where it is rather than pulling it down.
    /// </summary>
    public sealed class HitPointsRule() : TrackedValueRule(HitPointsKey, "PW", MaxHitPointsKey)
    {
        public override TrackedChangeResult Apply(int current, int maximum, NumberChange change) => change.Kind switch
        {
            NumberChangeKind.Subtract => Subtract(current, change.Amount),
            NumberChangeKind.Add => new TrackedChangeResult(Math.Max(current, Math.Min(current + change.Amount, maximum)), 0),
            NumberChangeKind.Set => new TrackedChangeResult(change.Amount, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(change), change.Kind, "Unknown change kind."),
        };

        private static TrackedChangeResult Subtract(int current, int amount)
        {
            var left = current - amount;

            // Already at or below 0 nothing more comes off; all of the amount is excess.
            var floor = Math.Min(current, 0);
            return left >= floor ? new TrackedChangeResult(left, 0) : new TrackedChangeResult(floor, floor - left);
        }
    }
}
