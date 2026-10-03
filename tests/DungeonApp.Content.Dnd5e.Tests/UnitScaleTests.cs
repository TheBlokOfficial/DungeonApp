using System;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// How a quantity is written on a card: Polish digits, no trailing zeros, the unit after a
/// non-breaking space, the largest unit the quantity is whole in.
/// </summary>
public sealed class UnitScaleTests
{
    [Theory]
    [InlineData("1.5", "1,5 kg")]
    [InlineData("0.25", "0,25 kg")]
    [InlineData("27.5", "27,5 kg")]
    [InlineData("3", "3 kg")]
    [InlineData("3.00", "3 kg")]
    [InlineData("1500", "1 500 kg")]
    public void A_weight_is_written_in_kilograms(string kilograms, string expected)
    {
        Assert.Equal(expected, Plain(UnitScale.Weight.Format(decimal.Parse(kilograms, System.Globalization.CultureInfo.InvariantCulture))));
    }

    [Theory]
    [InlineData("2", "1 t")]
    [InlineData("0.5", "1 h")]
    [InlineData("0.3", "3 m")]
    [InlineData("0.25", "2,5 m")]
    public void A_quantity_takes_the_largest_unit_it_is_whole_in_and_else_the_smallest(string quantity, string expected)
    {
        var scale = new UnitScale([new ScaleUnit("m", 0.1m), new ScaleUnit("t", 2m), new ScaleUnit("h", 0.5m)]);

        Assert.Equal(expected, Plain(scale.Format(decimal.Parse(quantity, System.Globalization.CultureInfo.InvariantCulture))));
    }

    [Fact]
    public void A_number_and_its_unit_never_part_across_lines()
    {
        Assert.Equal("1,5 kg", UnitScale.Weight.Format(1.5m));
    }

    [Fact]
    public void A_scale_without_units_is_refused()
    {
        Assert.Throws<ArgumentException>(() => new UnitScale([]));
    }

    // The group and unit separators are non-breaking spaces (U+00A0 or U+202F, depending on the
    // platform's culture data); the expectations above are written with plain ones.
    private static string Plain(string text) => text.Replace(' ', ' ').Replace(' ', ' ');
}
