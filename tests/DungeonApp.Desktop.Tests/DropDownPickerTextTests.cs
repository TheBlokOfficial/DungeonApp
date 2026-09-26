using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Tests;

public sealed class DropDownPickerTextTests
{
    [Fact]
    public void Nothing_selected_says_the_filter_name_without_a_badge_or_tip()
    {
        Assert.Equal("Rzadkość", DropDownPickerText.Label("Rzadkość", []));
        Assert.Equal(string.Empty, DropDownPickerText.MoreBadge([]));
        Assert.Null(DropDownPickerText.SelectionToolTip("Rzadkość", []));
    }

    [Fact]
    public void One_selected_says_its_name_without_a_badge_or_tip()
    {
        Assert.Equal("Rzadki", DropDownPickerText.Label("Rzadkość", ["Rzadki"]));
        Assert.Equal(string.Empty, DropDownPickerText.MoreBadge(["Rzadki"]));
        Assert.Null(DropDownPickerText.SelectionToolTip("Rzadkość", ["Rzadki"]));
    }

    [Fact]
    public void Three_selected_say_the_first_name_the_count_of_the_rest_and_all_in_the_tip()
    {
        string[] selected = ["Rzadki", "Bardzo rzadki", "Legendarny"];

        Assert.Equal("Rzadki", DropDownPickerText.Label("Rzadkość", selected));
        Assert.Equal("+2", DropDownPickerText.MoreBadge(selected));
        Assert.Equal("Rzadkość: Rzadki, Bardzo rzadki, Legendarny", DropDownPickerText.SelectionToolTip("Rzadkość", selected));
    }

    [Theory]
    [InlineData("Żywiołak ziemi", "żywioł", true)]
    [InlineData("żywiołak ziemi", "ŻYWIOŁ", true)]
    [InlineData("Pająk olbrzymi", "JĄK", true)]
    [InlineData("Goblin", "obl", true)]
    [InlineData("Goblin", "", true)]
    [InlineData("Goblin", null, true)]
    [InlineData("Goblin", "ork", false)]
    public void Matches_contains_regardless_of_letter_case(string text, string? query, bool expected) =>
        Assert.Equal(expected, DropDownPickerText.Matches(text, query));
}
