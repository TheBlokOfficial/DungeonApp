using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The creature statblock. <see cref="Dnd5eSystem.CreateCard"/> sets the model once, immediately
/// after construction, through <see cref="SetCreature"/>: the constructor stays parameterless, as the
/// XAML loader and the designer preview need it.
/// <para>
/// The portrait and the KP / PW / Szybkość values belong to the detail header
/// (<see cref="IEntryCardHeader"/>): the header draws the title and tags for every content type
/// alike, and this card only lends it those two pieces. The portrait is DungeonDetailPictureWidth
/// wide at 3:4, and the pairs' values start on the title column's line beside it.
/// </para>
/// <para>
/// Every value on the card comes from a field <see cref="Creature"/> declares, its
/// <see cref="CombatAspect"/> included; a pair or a section whose field is empty is not shown at
/// all. Ability modifiers are D&amp;D 5e's own arithmetic (<see cref="AbilityModifier"/>), tinted
/// by their sign.
/// </para>
/// </summary>
public partial class CreatureCardView : UserControl, IEntryCardHeader, IEntityLiveCard
{
    private const int HitPointsColumn = 0;
    private const int ArmorClassColumn = 1;

    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    private readonly ImageFrame _portrait;
    private readonly HeadlineValuesView _vitals = new();
    private readonly TrackGauge _hitPointsGauge = new() { IsVisible = false };
    private readonly StackPanel _headerBlock = new();
    private IReadOnlyList<StatTile> _vitalTiles = [];
    private Creature? _creature;
    private EntityLiveContext? _live;
    private TrackedValueTile? _hitPoints;

    public CreatureCardView()
    {
        InitializeComponent();

        // The gauge spans the title column over the values; hidden in the library, it takes no room.
        _headerBlock.Spacing = ThemeResource.Get<double>("DungeonSpacingMd");
        _headerBlock.Children.Add(_hitPointsGauge);
        _headerBlock.Children.Add(_vitals);

        var width = ThemeResource.Get<double>("DungeonDetailPictureWidth");
        _portrait = new ImageFrame
        {
            Width = width,
            Height = width * 4 / 3,
            VerticalAlignment = VerticalAlignment.Top,
            // Sharp, like the ability tables under it: one kind of corner on the card.
            CornerRadius = default,
            Icon = KindIcons.ForCreature(null),
        };

        // The pairs' values start on the title column's line: the label column and the pair's own
        // gap together span the portrait and the gap after it, a long label wrapping inside.
        Traits.LabelWidth = width + ThemeResource.Get<double>("DungeonDetailColumnGap") - ThemeResource.Get<double>("DungeonSpacingSm");
    }

    public Control HeaderVisual => _portrait;

    /// <summary>Nothing at the end of the title's line: a creature's name has the line to itself.</summary>
    public Control? HeaderTitleEnd => null;

    /// <summary>Nothing after the tags: size, type and alignment say it all.</summary>
    public Control? HeaderTagsStart => null;

    public Control HeaderBlock => _headerBlock;

    public void SetCreature(Creature creature, EntryPicture picture)
    {
        _creature = creature;

        // With no portrait, the frame shows what kind of creature this is.
        _portrait.Icon = KindIcons.ForCreature(creature.Type);
        picture.ShowIn(_portrait);

        var combat = creature.Combat;

        _vitalTiles = _vitals.Show(
        [
            new HeadlineValue("PW", "DungeonIconHeart", combat.Hp.ToString(CultureInfo.InvariantCulture), combat.HpDice),
            new HeadlineValue("KP", "DungeonIconShield", combat.Ac.ToString(CultureInfo.InvariantCulture), combat.AcSource),
            new HeadlineValue("Szybkość", "DungeonIconFootprints", creature.Speed),
        ]);

        ShowAbilities(key => new ShownScore(Score(combat, key), BaseDeviation.None));

        Traits.Rows = BuildTraitRows(creature);

        Show(SpecialAbilitiesSection, creature.SpecialAbilities);
        Show(ActionsSection, creature.Actions);
        Show(SpellcastingSection, creature.Spellcasting);
        Show(BonusActionsSection, creature.BonusActions);
        Show(ReactionsSection, creature.Reactions);
        Show(LegendaryActionsSection, creature.LegendaryActions);

        Footer.IsVisible = creature.Description is not null;
        DescriptionText.Text = creature.Description;
    }

    /// <summary>
    /// The pairs in statblock order. Zmysły and Wyzwanie are required fields, so they always show;
    /// every other pair only when its field is filled. Wyzwanie is the one value the GM looks for
    /// first, so it is drawn highlighted.
    /// </summary>
    private static IReadOnlyList<TraitRow> BuildTraitRows(Creature creature)
    {
        var rows = new List<TraitRow>();

        AddIfFilled(rows, "Rzuty obronne", creature.SavingThrows);
        AddIfFilled(rows, "Umiejętności", creature.Skills);
        AddIfFilled(rows, "Podatność na obrażenia", creature.DamageVulnerabilities);
        AddIfFilled(rows, "Odporność na obrażenia", creature.DamageResistances);
        AddIfFilled(rows, "Niepodatność na obrażenia", creature.DamageImmunities);
        AddIfFilled(rows, "Niepodatność na stany", creature.ConditionImmunities);
        rows.Add(new TraitRow("Zmysły", creature.Senses));
        AddIfFilled(rows, "Języki", creature.Languages);
        rows.Add(new TraitRow(
            "Wyzwanie",
            creature.Challenge,
            creature.Xp is { } xp ? $"{xp.ToString("N0", Polish)} PD" : null,
            IsHighlighted: true));

        return rows;
    }

    private static void AddIfFilled(List<TraitRow> rows, string label, string? value)
    {
        if (value is not null)
        {
            rows.Add(new TraitRow(label, value));
        }
    }

    /// <summary>
    /// The entity's numbers in the places of the entry's: current hit points against PW maks. as a
    /// gauge over the values and a change field in the PW tile, KP and the ability scores as shown
    /// (base plus effects) in the colour of their deviation, and the frame's note under the ability
    /// tables. Szybkość stays the entry's text - it has no base (<see cref="CreatureValues"/>).
    /// </summary>
    public void ShowLive(EntityLiveContext live)
    {
        ArgumentNullException.ThrowIfNull(live);

        _live = live;
        _hitPoints = new TrackedValueTile
        {
            Label = "PW",
            MaxDigits = 3,
            Icon = ThemeResource.Get<DrawingImage>("DungeonIconHeart"),
            Note = _creature?.Combat.HpDice,
            Preview = change => live.Part.FindTracked(CreatureValues.HitPointsKey)?.Preview(change) ?? string.Empty,
        };
        _hitPoints.ChangeAccepted += OnHitPointsChangeAccepted;
        _vitals.Place(HitPointsColumn, _hitPoints);
        _hitPointsGauge.IsVisible = true;

        LiveSections.Content = live.CreateSections();
        LiveSections.IsVisible = true;

        live.Changed += ShowLiveValues;
        ShowLiveValues();
    }

    // async void: the change is the end of a GM action, so its failure goes to the UI thread's handler.
    private async void OnHitPointsChangeAccepted(object? sender, NumberChangeEventArgs e) =>
        await _live!.ChangeAsync(CreatureValues.HitPointsKey, e.Change);

    private void ShowLiveValues()
    {
        var part = _live!.Part;

        if (part.FindTracked(CreatureValues.HitPointsKey) is { } hitPoints)
        {
            _hitPoints!.Value = FormatNumber(hitPoints.Current);
            _hitPoints.Maximum = FormatNumber(hitPoints.Maximum);
            _hitPointsGauge.Value = hitPoints.Current;
            _hitPointsGauge.Maximum = hitPoints.Maximum;
        }

        if (part.FindValue(CreatureValues.ArmorClassKey) is { } armorClass && _vitalTiles.Count > ArmorClassColumn)
        {
            _vitalTiles[ArmorClassColumn].Value = FormatNumber(armorClass.Shown);
            _vitalTiles[ArmorClassColumn].Deviation = BaseDeviations.Of(armorClass);
        }

        if (_creature is { } creature)
        {
            ShowAbilities(key => part.FindValue(key) is { } value
                ? new ShownScore(value.Shown, BaseDeviations.Of(value))
                : new ShownScore(Score(creature.Combat, key), BaseDeviation.None));
        }
    }

    private void ShowAbilities(Func<string, ShownScore> score)
    {
        PhysicalAbilities.Rows =
        [
            Ability("SIŁ", score(CreatureValues.StrKey)),
            Ability("ZRĘ", score(CreatureValues.DexKey)),
            Ability("KON", score(CreatureValues.ConKey)),
        ];
        MentalAbilities.Rows =
        [
            Ability("INT", score(CreatureValues.IntKey)),
            Ability("MDR", score(CreatureValues.WisKey)),
            Ability("CHA", score(CreatureValues.ChaKey)),
        ];
    }

    private static int Score(CombatAspect combat, string key) => key switch
    {
        CreatureValues.StrKey => combat.Str,
        CreatureValues.DexKey => combat.Dex,
        CreatureValues.ConKey => combat.Con,
        CreatureValues.IntKey => combat.Int,
        CreatureValues.WisKey => combat.Wis,
        CreatureValues.ChaKey => combat.Cha,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Not an ability score."),
    };

    // A number below zero carries the typographic minus, as every other number on the card.
    private static string FormatNumber(int number) =>
        number.ToString(CultureInfo.InvariantCulture).Replace('-', '−');

    private static AbilityRow Ability(string label, ShownScore shown)
    {
        var score = shown.Score;
        var modifier = AbilityModifier.Compute(score);
        var tone = modifier switch
        {
            > 0 => ValueTone.Positive,
            < 0 => ValueTone.Negative,
            _ => ValueTone.Neutral,
        };

        return new AbilityRow(label, score.ToString(CultureInfo.InvariantCulture), AbilityModifier.Format(score), tone, shown.Deviation);
    }

    /// <summary>An ability score as the card shows it, with where it stands against its base.</summary>
    private readonly record struct ShownScore(int Score, BaseDeviation Deviation);

    private static void Show(ProseSectionView view, StatblockSection? section)
    {
        view.IsVisible = section is not null;
        view.Intro = section?.Intro;
        view.Items = section is null
            ? []
            : [.. section.Entries.Select(entry => new ProseItem(entry.Name, entry.Note, entry.Text))];
    }
}
