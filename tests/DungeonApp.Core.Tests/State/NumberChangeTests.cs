using DungeonApp.Core.State;

namespace DungeonApp.Core.Tests.State;

public sealed class NumberChangeTests
{
    [Theory]
    [InlineData("-5", NumberChangeKind.Subtract, 5)]
    [InlineData("−12", NumberChangeKind.Subtract, 12)]
    [InlineData("+3", NumberChangeKind.Add, 3)]
    [InlineData("=10", NumberChangeKind.Set, 10)]
    [InlineData("=-4", NumberChangeKind.Set, -4)]
    [InlineData("  - 7 ", NumberChangeKind.Subtract, 7)]
    [InlineData("-0", NumberChangeKind.Subtract, 0)]
    [InlineData("12", NumberChangeKind.Set, 12)]
    [InlineData(" 0 ", NumberChangeKind.Set, 0)]
    public void Reads_a_change(string text, NumberChangeKind kind, int amount)
    {
        Assert.True(NumberChange.TryParse(text, out var change));
        Assert.Equal(new NumberChange(kind, amount), change);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1-2")]
    [InlineData("-")]
    [InlineData("+")]
    [InlineData("=")]
    [InlineData("--5")]
    [InlineData("+-5")]
    [InlineData("-5a")]
    [InlineData("-2,5")]
    [InlineData("-99999999999")]
    public void Refuses_anything_but_an_optional_sign_and_a_whole_number(string? text)
    {
        Assert.False(NumberChange.TryParse(text, out _));
    }

    [Theory]
    [InlineData(NumberChangeKind.Subtract, 5, "−5")]
    [InlineData(NumberChangeKind.Add, 3, "+3")]
    [InlineData(NumberChangeKind.Set, 10, "=10")]
    [InlineData(NumberChangeKind.Set, -4, "=−4")]
    public void Shows_the_change_with_the_typographic_minus(NumberChangeKind kind, int amount, string expected)
    {
        Assert.Equal(expected, new NumberChange(kind, amount).ToString());
    }
}
