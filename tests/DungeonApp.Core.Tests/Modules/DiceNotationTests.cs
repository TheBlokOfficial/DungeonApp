using DungeonApp.Core;
using DungeonApp.Core.Modules.Dice;

namespace DungeonApp.Core.Tests.Modules;

public sealed class DiceNotationTests
{
    [Theory]
    [InlineData("k20", 1, 20, 0)]
    [InlineData("d20", 1, 20, 0)]
    [InlineData("K20", 1, 20, 0)]
    [InlineData("2k6", 2, 6, 0)]
    [InlineData("2k6+3", 2, 6, 3)]
    [InlineData("4k6-1", 4, 6, -1)]
    [InlineData("  2 k 6 + 3  ", 2, 6, 3)]
    [InlineData("k100", 1, 100, 0)]
    public void Reads_a_roll_the_way_a_gm_writes_it(string value, int count, int sides, int modifier)
    {
        Assert.True(DiceNotation.TryParse(value, out var notation));
        Assert.Equal(new DiceNotation(count, sides, modifier), notation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("20")]
    [InlineData("k")]
    [InlineData("k0")]
    [InlineData("k1")]
    [InlineData("0k6")]
    [InlineData("k20+")]
    [InlineData("k20+3+3")]
    [InlineData("2k6 albo 3k4")]
    [InlineData("101k6")]
    [InlineData("k1001")]
    [InlineData("k20+1001")]
    public void Refuses_what_is_not_a_roll(string value)
        => Assert.False(DiceNotation.TryParse(value, out _));

    [Fact]
    public void Refuses_a_mistyped_roll_with_an_example_rather_than_a_diagnosis()
    {
        var refusal = Assert.Throws<CampaignRuleException>(() => DiceNotation.Parse("dwadzieścia"));

        Assert.Contains("k20", refusal.Message, System.StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1, 20, 0, "k20")]
    [InlineData(2, 6, 3, "2k6+3")]
    [InlineData(4, 6, -1, "4k6-1")]
    public void Writes_itself_back_the_way_it_was_meant(int count, int sides, int modifier, string expected)
        => Assert.Equal(expected, new DiceNotation(count, sides, modifier).ToString());
}
