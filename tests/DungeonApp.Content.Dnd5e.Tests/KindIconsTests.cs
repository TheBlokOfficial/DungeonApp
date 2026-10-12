using System.Linq;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// Which icon a creature's type and an item's category get: the base type before a subtype, the
/// category ignoring case, and the content type's default for a word this system does not know.
/// </summary>
public sealed class KindIconsTests
{
    [Theory]
    [InlineData("humanoid", "humanoid")]
    [InlineData("humanoid (dowolna rasa)", "humanoid")]
    [InlineData("czart (diabeł)", "czart")]
    [InlineData("Nieumarły", "nieumarly")]
    [InlineData("  smok ", "smok")]
    [InlineData("żywiołak", "zywiolak")]
    [InlineData("rój Malutkich bestii", "bestia")]
    public void A_creature_type_gets_the_icon_of_its_base_type(string type, string expected) =>
        Assert.Equal(expected, KindIcons.CreatureIconName(type));

    [Theory]
    [InlineData("kosmita")]
    [InlineData("")]
    [InlineData(null)]
    public void An_unknown_creature_type_gets_the_default_icon(string? type) =>
        Assert.Equal(KindIcons.DefaultCreatureIcon, KindIcons.CreatureIconName(type));

    [Theory]
    [InlineData("Broń", "bron")]
    [InlineData("broń", "bron")]
    [InlineData("Przedmiot cudowny", "przedmiot-cudowny")]
    [InlineData("Różdżka", "rozdzka")]
    public void An_item_category_gets_its_icon_ignoring_case(string category, string expected) =>
        Assert.Equal(expected, KindIcons.GearIconName(category));

    [Theory]
    [InlineData("Artefakt obcy")]
    [InlineData("")]
    [InlineData(null)]
    public void An_unknown_item_category_gets_the_default_icon(string? category) =>
        Assert.Equal(KindIcons.DefaultGearIcon, KindIcons.GearIconName(category));

    /// <summary>Every icon the lists name, the defaults included, is embedded in the assembly.</summary>
    [Fact]
    public void Every_named_icon_is_embedded()
    {
        var embedded = typeof(KindIcons).Assembly.GetManifestResourceNames();
        var named = KindIcons.CreatureTypes.Select(type => type.Icon)
            .Concat(KindIcons.GearCategories.Select(category => category.Icon))
            .Append(KindIcons.DefaultCreatureIcon)
            .Append(KindIcons.DefaultGearIcon);

        Assert.All(named, icon => Assert.Contains($"DungeonApp.Content.Dnd5e.Icons.{icon}.svg", embedded));
    }
}
