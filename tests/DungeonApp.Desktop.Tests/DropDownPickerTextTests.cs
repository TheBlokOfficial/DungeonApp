using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Tests;

public sealed class DropDownPickerTextTests
{
    [Fact]
    public void Summary_of_nothing_selected_is_empty_so_the_placeholder_shows() =>
        Assert.Equal(string.Empty, DropDownPickerText.Summary([]));

    [Fact]
    public void Summary_of_one_selected_is_its_name() =>
        Assert.Equal("Humanoid", DropDownPickerText.Summary(["Humanoid"]));

    [Fact]
    public void Summary_of_three_selected_is_the_first_name_and_the_count_of_the_rest() =>
        Assert.Equal("Humanoid +2", DropDownPickerText.Summary(["Humanoid", "Nieumarły", "Smok"]));

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
