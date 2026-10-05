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
/// as wide as the creature's portrait (DungeonDetailPictureWidth), so every card's title starts on
/// the same line; the weight and, under it, the worth at the end of the title's line; the rarity pill at the
/// start of the tags' row, drawn like the list's (the tags after it - "magiczny", the subtype - are
/// the profile's, <see cref="Dnd5eSystem"/>); and the KP / Obrażenia / Ładunki headline values,
/// the same block as the creature's KP / PZ / Szybkość (<see cref="HeadlineValuesView"/>). An item
/// with none of those three lends no block; weight and worth every item has.
/// </para>
/// <para>
/// Every value on the card comes from a field <see cref="Gear"/> declares; a value whose field is
/// empty is not shown, while a zero is a value and is written. An icon stands only by a headline value, never in the pairs. Nothing is
/// computed: the weight is only written in its scale (<see cref="UnitScale.Weight"/>), the worth as
/// a bare number (<see cref="PolishNumber"/>).
/// </para>
/// </summary>
public partial class GearCardView : UserControl, IEntryCardHeader
{
    private readonly ImageFrame _picture;
    private readonly WordTag _rarity = new();
    private readonly GearMetadataView _metadata = new();
    private HeadlineValuesView? _headline;

    public GearCardView()
    {
        InitializeComponent();

        var side = ThemeResource.Get<double>("DungeonDetailPictureWidth");
        _picture = new ImageFrame
        {
            Width = side,
            Height = side,
            VerticalAlignment = VerticalAlignment.Top,
            // Sharp, like every bordered block on a card.
            CornerRadius = default,
            Icon = ThemeResource.Get<DrawingImage>("DungeonIconBackpack"),
        };

        // The pairs' values start on the title column's line, as on the creature card: the label
        // column and the pair's own gap together span the picture and the gap after it.
        Traits.LabelWidth = side + ThemeResource.Get<double>("DungeonDetailColumnGap") - ThemeResource.Get<double>("DungeonSpacingSm");
    }

    public Control HeaderVisual => _picture;

    public Control HeaderTitleEnd => _metadata;

    public Control HeaderTagsStart => _rarity;

    public Control? HeaderBlock => _headline;

    /// <summary>
    /// The square picture leaves room beside it for one line of title, the tags and the block, so a
    /// long name is trimmed and shown whole under the pointer instead of pushing the header taller.
    /// </summary>
    public bool HeaderTitleOnOneLine => true;

    public void SetGear(Gear gear, EntryPicture picture)
    {
        picture.ShowIn(_picture);

        _metadata.Show(UnitScale.Weight.Format(gear.Item.Weight), $"wartość {PolishNumber.Format(gear.Item.Value)}");

        ShowRarity(gear.Rarity);

        var headline = new List<HeadlineValue>();
        if (gear.ArmorClass is { } armorClass)
        {
            headline.Add(new HeadlineValue("KP", "DungeonIconShield", armorClass, gear.ArmorClassNote));
        }

        if (gear.Damage is { } damage)
        {
            headline.Add(new HeadlineValue("Obrażenia", "DungeonIconSword", damage, gear.DamageType));
        }

        if (gear.Charges is { } charges)
        {
            headline.Add(new HeadlineValue("Ładunki", "DungeonIconZap", charges.Max.ToString(CultureInfo.InvariantCulture)));
        }

        _headline = headline.Count > 0 ? new HeadlineValuesView() : null;
        _headline?.Show(headline);

        var traits = Filled(
            ("Dostrojenie", gear.Attunement ? Attunement(gear.AttunementBy) : null),
            ("Właściwości", gear.Properties),
            ("Siła", gear.StrengthRequirement?.ToString(CultureInfo.InvariantCulture)),
            ("Skradanie się", gear.StealthDisadvantage ? "utrudnienie" : null),
            ("Odnawianie", gear.Charges?.Recharge));
        TraitsBlock.IsVisible = traits.Count > 0;
        Traits.Rows = traits;

        DescriptionBlock.IsVisible = gear.Description is not null;
        DescriptionSection.Intro = gear.Description;
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
}
