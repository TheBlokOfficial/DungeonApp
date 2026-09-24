namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// D&amp;D 5e's own ability-modifier arithmetic: floor((score - 10) / 2), and the "+2"/"−1"/"+0" text
/// the monster card shows next to a score (krok 10, brief C). Pure arithmetic over a value the
/// entry's own record already carries - a derived value, docs/architecture.md's "Poziomy logiki i
/// formuły" own first level, not a rule the Mistrz Gry would otherwise apply by hand.
/// </summary>
public static class AbilityModifier
{
    /// <summary>The signed modifier for a raw ability score, D&amp;D 5e's own floor((score-10)/2).</summary>
    public static int Compute(int score) =>
        (int)System.Math.Floor((score - 10) / 2.0);

    /// <summary>
    /// The modifier formatted the way a statblock prints it: always signed, "+2" or "+0", using the
    /// proper minus sign (U+2212, matching the mockup's own "−1") rather than a hyphen for a negative
    /// value.
    /// </summary>
    public static string Format(int score)
    {
        var modifier = Compute(score);

        return modifier < 0 ? $"−{-modifier}" : $"+{modifier}";
    }
}
