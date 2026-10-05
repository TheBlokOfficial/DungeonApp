namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The combat aspect: the typed fields a fight reads - armor class, hit points and the six ability
/// scores. Data without a look: the card of the type that carries it decides where each field
/// stands (<see cref="CreatureCardView"/> puts KP and PZ by the portrait, the scores in the ability
/// tables). Optional on a <see cref="Creature"/>; a creature without it shows none of these.
/// </summary>
public sealed record CombatAspect
{
    public required int Ac { get; init; }

    /// <summary>What the armor class comes from ("pancerz naturalny"), written under it.</summary>
    public string? AcSource { get; init; }

    /// <summary>The maximum hit points.</summary>
    public required int Hp { get; init; }

    /// <summary>The hit dice the maximum is rolled with ("2k8+2"), written under it.</summary>
    public string? HpDice { get; init; }

    /// <summary>
    /// Current hit points for one specimen. An entry only ever declares the maximum in
    /// <see cref="Hp"/>; this is filled in by an instance's patch, so it stays null until something
    /// happens to a particular goblin.
    /// </summary>
    public int? CurrentHp { get; init; }

    public required int Str { get; init; }

    public required int Dex { get; init; }

    public required int Con { get; init; }

    public required int Int { get; init; }

    public required int Wis { get; init; }

    public required int Cha { get; init; }
}
