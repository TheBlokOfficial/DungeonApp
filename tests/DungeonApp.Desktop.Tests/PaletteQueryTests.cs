using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Tests;

public sealed class PaletteQueryTests
{
    [Theory]
    [InlineData("4 gob", 4, "gob")]
    [InlineData("  12   gob lin", 12, "gob lin")]
    [InlineData("99 gob", 99, "gob")]
    [InlineData("1 gob", 1, "gob")]
    [InlineData("4 ", 4, "")]
    public void A_leading_number_and_space_is_the_quantity(string text, int quantity, string query)
    {
        Assert.Equal((quantity, query), PaletteQuery.Parse(text, allowQuantity: true));
    }

    [Theory]
    [InlineData("gob")]
    [InlineData("4")]
    [InlineData("4gob")]
    [InlineData("0 gob")]
    [InlineData("100 gob")]
    [InlineData("-3 gob")]
    [InlineData("2,5 gob")]
    [InlineData("99999999999 gob")]
    [InlineData("")]
    public void Anything_else_is_the_whole_query(string text)
    {
        Assert.Equal((1, text), PaletteQuery.Parse(text, allowQuantity: true));
    }

    [Fact]
    public void Quantity_is_ignored_when_not_allowed()
    {
        Assert.Equal((1, "4 gob"), PaletteQuery.Parse("4 gob", allowQuantity: false));
        Assert.Equal((1, ""), PaletteQuery.Parse(null, allowQuantity: true));
    }
}
