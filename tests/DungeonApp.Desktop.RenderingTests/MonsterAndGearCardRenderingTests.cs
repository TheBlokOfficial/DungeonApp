using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using DungeonApp.Content.Dnd5e;
using DungeonApp.Library.Entries.Desktop.Controls.Content;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Headless-rendering coverage for the monster and gear cards' own redesign (krok 10, brief C) and
/// the "no duplicate rows" rule both cards share (brief A10). Builds each card directly - neither
/// needs a <c>Dnd5eSystem</c> or a registry, just <c>SetMonster</c>/<c>SetGear</c> on a freshly
/// constructed view, same as <c>Dnd5eSystem.CreateCard</c> itself does.
/// </summary>
public sealed class MonsterAndGearCardRenderingTests
{
    private static Monster MakeMonster(
        string? group = "Goblinoidy",
        string? acSource = "skórz., tarcza",
        string? hpDice = "2k6",
        string? skills = "Skradanie się +6",
        string? languages = "wspólny, goblini",
        string? specialAbilities = "Zwinna ucieczka. Może uciec.",
        string? description = "Mały, tchórzliwy rabuś.") => new()
    {
        Size = "Mały",
        Type = "humanoid (goblinoid)",
        Group = group,
        Alignment = "neutralny zły",
        Ac = 15,
        AcSource = acSource,
        Hp = 7,
        HpDice = hpDice,
        Speed = "30 stóp",
        Str = 8,
        Dex = 14,
        Con = 10,
        Int = 10,
        Wis = 8,
        Cha = 8,
        Skills = skills,
        Senses = "widzenie w ciemności 60 stóp",
        Languages = languages,
        Challenge = "1/4",
        SpecialAbilities = specialAbilities,
        Actions = "Bułat. Atak bronią w zwarciu: +4 do trafienia.",
        Description = description,
    };

    private static MonsterCardView BuildMonsterCard(Monster? monster = null)
    {
        var view = new MonsterCardView();
        view.SetMonster(monster ?? MakeMonster());

        var window = new Window { Content = view, Width = 720, Height = 1400 };
        window.Show();
        window.GetLayoutManager()!.ExecuteLayoutPass();

        return view;
    }

    private static IReadOnlyList<TextBlock> AllTextBlocks(Control root) =>
        [.. root.GetVisualDescendants().OfType<TextBlock>()];

    // -----------------------------------------------------------------------------------------
    // Brief A10: no duplicate rows - tags the detail header already shows (size/type/alignment,
    // rarity) never repeat inside the card itself.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Fails against the pre-redesign card (ec09015), whose own SizeTypeAlignment TraitListView drew
    /// exactly these three rows under these three labels - the same three values the detail header's
    /// own Tags already show for a monster (Dnd5eSystem.BuildMonsterContentTab: size, type, alignment).
    /// </summary>
    [AvaloniaFact]
    public void Monster_card_never_repeats_size_type_or_alignment_as_its_own_row()
    {
        var view = BuildMonsterCard();

        var labels = AllTextBlocks(view).Select(tb => tb.Text).ToArray();

        Assert.DoesNotContain("Rozmiar", labels);
        Assert.DoesNotContain("Typ", labels);
        Assert.DoesNotContain("Charakter", labels);
    }

    /// <summary>Fails against the pre-fix GearCardView.SetGear, whose Properties.Rows always led with a "Rzadkość" TraitRow - the same value already shown as this content type's only tag.</summary>
    [AvaloniaFact]
    public void Gear_card_never_repeats_rarity_as_its_own_row()
    {
        var view = new GearCardView();
        view.SetGear(new Gear { Rarity = "Niezwykły", Weight = 3, Description = "Ostre." });

        var window = new Window { Content = view, Width = 400, Height = 400 };
        window.Show();
        window.GetLayoutManager()!.ExecuteLayoutPass();

        var labels = AllTextBlocks(view).Select(tb => tb.Text).ToArray();

        // The rarity VALUE ("Niezwykły") is gone from the card entirely, not just its label - the
        // header's own tag (drawn by ContentTabView, not this card) is the only place it shows now.
        Assert.DoesNotContain("Rzadkość", labels);
        Assert.DoesNotContain("Niezwykły", labels);
    }

    // -----------------------------------------------------------------------------------------
    // Brief C: the new statblock shape.
    // -----------------------------------------------------------------------------------------

    [AvaloniaFact]
    public void Ability_grid_shows_all_six_scores_with_5e_modifiers()
    {
        var view = BuildMonsterCard();

        var rows = view.Abilities.Rows.ToArray();

        Assert.Equal(
        [
            new AbilityRow("SIŁ", "8", "−1"),
            new AbilityRow("ZRĘ", "14", "+2"),
            new AbilityRow("KON", "10", "+0"),
            new AbilityRow("INT", "10", "+0"),
            new AbilityRow("MDR", "8", "−1"),
            new AbilityRow("CHA", "8", "−1"),
        ],
            rows);
    }

    [AvaloniaFact]
    public void Challenge_renders_as_a_chip_not_a_plain_trait_row()
    {
        var view = BuildMonsterCard();

        Assert.Equal("1/4", view.ChallengeText.Text);
        var chip = view.ChallengeText.GetVisualAncestors().OfType<Border>().First();
        Assert.True(chip.CornerRadius.TopLeft > 0, "Wyzwanie nie jest w chipie z zaokrąglonym rogiem.");

        // Never one more row inside the plain skills/senses list.
        Assert.DoesNotContain(view.SkillsAndSenses.Rows, row => row.Label == "Wyzwanie");
    }

    [AvaloniaFact]
    public void Stat_strip_shows_ac_hp_and_speed_with_their_own_notes()
    {
        var view = BuildMonsterCard();

        Assert.Equal("15", view.AcValueRun.Text);
        Assert.Equal(" (skórz., tarcza)", view.AcNoteRun.Text);
        Assert.Equal("7", view.HpValueRun.Text);
        Assert.Equal(" (2k6)", view.HpNoteRun.Text);
        Assert.Equal("30 stóp", view.SpeedValueText.Text);
    }

    [AvaloniaFact]
    public void A_monster_with_no_special_abilities_hides_that_section_entirely()
    {
        var view = BuildMonsterCard(MakeMonster(specialAbilities: null));

        Assert.False(view.SpecialAbilitiesSection.IsVisible);
    }

    [AvaloniaFact]
    public void A_monster_with_no_description_hides_that_section_entirely()
    {
        var view = BuildMonsterCard(MakeMonster(description: null));

        Assert.False(view.DescriptionSection.IsVisible);
    }
}
