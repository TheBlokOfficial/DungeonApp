using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Desktop.Tests.Entries;

/// <summary>
/// The period a prose item's name line closes with: the view adds it, so a pack writes bare names,
/// and it never doubles punctuation the name already ends with.
/// </summary>
public sealed class ProseItemTests
{
    [Fact]
    public void A_bare_name_is_closed_with_a_period()
    {
        Assert.Equal(".", new ProseItem("Ugryzienie", null, "Atak.").Period);
    }

    [Theory]
    [InlineData("Ugryzienie.")]
    [InlineData("Na pomoc!")]
    [InlineData("Kto tam?")]
    [InlineData("Bez ograniczeń:")]
    [InlineData("I wtedy…")]
    public void A_name_ending_in_punctuation_gets_no_period(string name)
    {
        Assert.Null(new ProseItem(name, null, "Tekst.").Period);
    }

    [Fact]
    public void With_a_note_the_period_closes_the_line_after_the_note()
    {
        var item = new ProseItem("Leczący dotyk", "3 na dzień", "Tekst.");

        Assert.Equal(" (3 na dzień)", item.NoteDisplay);
        Assert.Equal(".", item.Period);
    }
}
