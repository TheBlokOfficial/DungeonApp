using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.Controls;
using DungeonApp.Desktop.Entries.Converters;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The item card. <see cref="Dnd5eSystem.CreateCard"/> sets the model once, immediately after
/// construction, through <see cref="SetGear"/>: the constructor stays parameterless, as the XAML
/// loader and the designer preview need it.
/// <para>
/// Four pieces are lent to the detail header (<see cref="IEntryCardHeader"/>): the square picture -
/// three ability cells wide, like the monster's portrait, so every card's title starts on the same
/// line; the weight at the end of the title's line; the rarity pill at the start of the tags' row,
/// before the subtype (<see cref="Dnd5eSystem"/>), drawn like the list's; and the Wartość / Dostrojenie pairs
/// under them. An item with neither of those two pairs lends no block, one without a weight no
/// title end.
/// </para>
/// <para>
/// Every value on the card comes from a field <see cref="Gear"/> declares; a pair whose field is
/// empty is not shown, nor is a group with no pair. Nothing is computed: the weight is only written
/// in its scale (<see cref="UnitScale.Weight"/>), the worth as a bare number (<see cref="PolishNumber"/>).
/// </para>
/// </summary>
public partial class GearCardView : UserControl, IEntryCardHeader
{
    private readonly ImageFrame _picture;
    private readonly WordTag _rarity = new();
    private GearWeightView? _weight;
    private TraitListView? _headline;

    public GearCardView()
    {
        InitializeComponent();

        var cell = ThemeResource.Get<double>("DungeonAbilityCellSize");
        var side = 3 * cell;
        _picture = new ImageFrame
        {
            Width = side,
            Height = side,
            VerticalAlignment = VerticalAlignment.Top,
            // Sharp, like every bordered block on a card.
            CornerRadius = default,
            Icon = ThemeResource.Get<DrawingImage>("DungeonIconBackpack"),
        };

        // The pairs' values start on the title column's line, as on the monster card: the label
        // column and the pair's own gap together span the picture and the gap after it.
        var labelWidth = side + ThemeResource.Get<double>("DungeonDetailColumnGap") - ThemeResource.Get<double>("DungeonSpacingSm");
        WeaponTraits.LabelWidth = labelWidth;
        ArmorTraits.LabelWidth = labelWidth;
        ChargeTraits.LabelWidth = labelWidth;
    }

    public Control HeaderVisual => _picture;

    public Control? HeaderTitleEnd => _weight;

    public Control HeaderTagsStart => _rarity;

    public Control? HeaderBlock => _headline;

    public void SetGear(Gear gear, EntryPicture picture)
    {
        picture.ShowIn(_picture);

        _weight = gear.Weight is { } weight ? new GearWeightView { Text = UnitScale.Weight.Format(weight) } : null;

        ShowRarity(gear.Rarity);

        var headline = Filled(
            ("Wartość", gear.Value is { } value ? PolishNumber.Format(value) : null),
            ("Dostrojenie", gear.Attunement ? Attunement(gear.AttunementBy) : null));
        _headline = headline.Count > 0 ? new TraitListView { Rows = headline } : null;

        Show(WeaponTraits, Filled(
            ("Obrażenia", gear.Damage),
            ("Właściwości", gear.Properties)));

        var armor = new List<TraitRow>();
        if (gear.ArmorClass is { } armorClass)
        {
            armor.Add(new TraitRow("KP", armorClass, Icon: ThemeResource.Get<DrawingImage>("DungeonIconShield")));
        }

        armor.AddRange(Filled(
            ("Siła", gear.StrengthRequirement?.ToString(CultureInfo.InvariantCulture)),
            ("Skradanie się", gear.StealthDisadvantage ? "utrudnienie" : null)));
        Show(ArmorTraits, armor);

        Show(ChargeTraits, Filled(
            ("Ładunki", gear.Charges?.ToString(CultureInfo.InvariantCulture)),
            ("Odnawianie", gear.Recharge)));

        Groups.IsVisible = Groups.Children.Any(group => group.IsVisible);
        DescriptionBlock.IsVisible = gear.Description is not null;
        DescriptionText.Text = gear.Description;
    }

    /// <summary>
    /// The rarity pill in its tier's color - its text in the color, its fill the same color dimmed,
    /// exactly like the list's badge; a rarity this system does not know is a plain word tag.
    /// </summary>
    private void ShowRarity(string rarity)
    {
        _rarity.Content = rarity;
        if (Dnd5eSystem.RarityBrush(rarity) is { } brush)
        {
            _rarity.Classes.Add("custom");
            _rarity.Foreground = brush;
            _rarity.Background = (IBrush?)DimBrushConverter.Instance.Convert(brush, typeof(IBrush), null, CultureInfo.InvariantCulture);
        }
    }

    private static string Attunement(string? by) => by is null ? "wymagane" : $"wymagane {by}";

    private static List<TraitRow> Filled(params (string Label, string? Value)[] pairs) =>
        [.. pairs.Where(pair => pair.Value is not null).Select(pair => new TraitRow(pair.Label, pair.Value!))];

    private static void Show(TraitListView view, IReadOnlyList<TraitRow> rows)
    {
        view.IsVisible = rows.Count > 0;
        view.Rows = rows;
    }
}
