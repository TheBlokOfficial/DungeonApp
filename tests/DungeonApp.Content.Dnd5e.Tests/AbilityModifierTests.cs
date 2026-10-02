namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// D&amp;D 5e's own floor((score-10)/2) on several values - values below 10 (negative
/// modifiers), odd scores (5e's own case for floor rounding down, not truncating toward zero), and
/// the boundary at 10/11 (both give +0).
/// </summary>
public sealed class AbilityModifierTests
{
    [Theory]
    [InlineData(1, -5)]
    [InlineData(3, -4)]
    [InlineData(8, -1)]
    [InlineData(9, -1)]
    [InlineData(10, 0)]
    [InlineData(11, 0)]
    [InlineData(12, 1)]
    [InlineData(14, 2)]
    [InlineData(15, 2)]
    [InlineData(20, 5)]
    public void Computes_the_5e_floor_modifier(int score, int expected)
    {
        Assert.Equal(expected, AbilityModifier.Compute(score));
    }

    [Theory]
    [InlineData(8, "−1")]
    [InlineData(10, "+0")]
    [InlineData(11, "+0")]
    [InlineData(14, "+2")]
    [InlineData(1, "−5")]
    public void Formats_the_modifier_signed_with_a_proper_minus_sign(int score, string expected)
    {
        Assert.Equal(expected, AbilityModifier.Format(score));
    }
}
