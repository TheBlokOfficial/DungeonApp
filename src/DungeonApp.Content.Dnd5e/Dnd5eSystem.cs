using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Instances;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.ContentTab;
using DungeonApp.Desktop.Workspace.Controls;
using DungeonApp.Desktop.Workspace;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Desktop.Workspace.Panels;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The one place in the application allowed to know what a monster or a piece of gear is. Declares
/// two content types - <c>monster</c> ("Potwór", version 1) and <c>gear</c> ("Przedmiot", version 1)
/// - builds their cards, and declares this system's tabs: "Potwory" and "Przedmioty" in the System
/// category, "Biurko" in the Campaign category.
/// <para>
/// Every content type is one row of a table, looked up by its reference in <see cref="TryGet"/>,
/// <see cref="TryValidate"/> and <see cref="CreateCard"/>. Knowing what each type is belongs here and
/// nowhere else: <c>CoreEntryKindIndependenceTests</c> bans <c>Core</c> and <c>Desktop</c> from
/// branching on entry kind, and neither of them names a content type.
/// </para>
/// <para>
/// Carries two distinct identities on purpose: <see cref="Id"/> is what the frame knows this system as (<see cref="SystemId"/>, never
/// <c>DungeonApp.Core.Entries</c>'s own <see cref="ContentId"/>); <see cref="ContentSetId"/> is the
/// content-set id every content type reference and pack entry in this system actually points at. Both
/// are minted from the same literal, but nothing enforces that they stay equal - a system is free to
/// pick a different one for either.
/// </para>
/// </summary>
public sealed class Dnd5eSystem : IGameSystem, IContentTypeCatalog, IContentPresentation
{
    // Internal, not private: InstanceRowViewModel's hit-point editing needs the same id to decide
    // whether a row is a monster, and branching on this id is allowed only inside this system -
    // duplicating the literal there instead would let the two silently drift.
    internal const string MonsterTypeId = "monster";
    private const string GearTypeId = "gear";

    /// <summary>The JSON name of <see cref="Monster.Image"/> and <see cref="Gear.Image"/>.</summary>
    private const string ImagePropertyName = "image";

    // Public, not internal: DungeonApp.App/Program.cs - the one place allowed to name a system by
    // name - is a separate assembly, and reads this to compute this system's packs directory from
    // its own identifier, rather than typing the literal "dnd5e" a second time somewhere the two
    // could drift apart.
    public const string IdValue = "dnd5e";

    /// <summary>
    /// Rarity tiers in display order - the scale of computer games, not the rulebook's - paired with
    /// the color-key name their badge carries: an opaque intent for <see cref="ContentBadge.ColorKey"/>,
    /// not a color; <see cref="ResolveBadgeBrush"/> is the only place that turns it into one. D&amp;D's
    /// rarities map onto it as: mundane and common → Pospolity, uncommon → Niepospolity, rare → Rzadki,
    /// very rare → Epicki, legendary and artifact → Legendarny. A rarity string this system does not
    /// recognise gets no color key and falls after every known tier when "Rzadkość" is ordered
    /// (see <see cref="RankedTextComparer"/>).
    /// </summary>
    private static readonly IReadOnlyList<(string Tier, string ColorKey)> RarityTiers =
    [
        ("Pospolity", "rarity-common"),
        ("Niepospolity", "rarity-uncommon"),
        ("Rzadki", "rarity-rare"),
        ("Epicki", "rarity-epic"),
        ("Legendarny", "rarity-legendary"),
    ];

    private static readonly IComparer<string> RarityOrder = new RankedTextComparer([.. RarityTiers.Select(tier => tier.Tier)]);

    private static readonly IReadOnlyDictionary<string, string> RarityColorKeys =
        RarityTiers.ToDictionary(tier => tier.Tier, tier => tier.ColorKey, StringComparer.Ordinal);

    /// <summary>
    /// The brush each rarity color key resolves to (<see cref="ResolveBadgeBrush"/>) - this system's
    /// own colors, minted fresh rather than borrowed from the frame's meaning-carrying tokens: a scale
    /// that belongs to a system carries its own colors. Hues follow the computer-game rarity convention
    /// (light gray, green, blue, violet, orange) and none is the accent color. Each is bright enough
    /// for its text to keep at least 4.5:1 against its own dimmed pill (the same color at 16%) laid on
    /// the list row at rest, under the pointer and selected.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IBrush> RarityBrushes = new Dictionary<string, IBrush>(StringComparer.Ordinal)
    {
        // Rzadkość: pospolity.
        ["rarity-common"] = new SolidColorBrush(Color.Parse("#D2D0C8")),
        // Rzadkość: niepospolity.
        ["rarity-uncommon"] = new SolidColorBrush(Color.Parse("#66C270")),
        // Rzadkość: rzadki.
        ["rarity-rare"] = new SolidColorBrush(Color.Parse("#7AAAD6")),
        // Rzadkość: epicki.
        ["rarity-epic"] = new SolidColorBrush(Color.Parse("#BA9EDC")),
        // Rzadkość: legendarny.
        ["rarity-legendary"] = new SolidColorBrush(Color.Parse("#E8A060")),
    };

    private const string NeedsAttunement = "wymaga";
    private const string NeedsNoAttunement = "nie wymaga";

    /// <summary>The "Dostrojenie" filter's two options, the one that narrows the list first.</summary>
    private static readonly IComparer<string> AttunementOrder = new RankedTextComparer([NeedsAttunement, NeedsNoAttunement]);

    /// <summary>The magical item's pill on the card, and the "Magia" filter's option for it.</summary>
    private const string MagicalTag = "magiczny";
    private const string Mundane = "niemagiczny";

    /// <summary>The "Magia" filter's two options, the one that narrows the list first.</summary>
    private static readonly IComparer<string> MagicOrder = new RankedTextComparer([MagicalTag, Mundane]);

    private static readonly IComparer<string> ChallengeOrder = new ChallengeOrderComparer();

    /// <summary>The "Typ" filter's option order: alphabetical, Polish collation, case insensitive.</summary>
    private static readonly IComparer<string> PolishAlphabeticalOrder =
        StringComparer.Create(new System.Globalization.CultureInfo("pl-PL"), ignoreCase: true);

    private readonly WorkspaceLayoutStore _layoutStore;
    private readonly IReadOnlyDictionary<ContentTypeReference, ContentTypeRegistration> _contentTypes;
    private readonly LoadContentPacksStep _loadPacksStep;

    /// <param name="layoutStore">Where the desk's layout is kept.</param>
    /// <param name="packsPaths">
    /// Every directory this system's packs are read from - the GM's own and the ones shipped with the
    /// program - as the composition root computed them. Read as one scan into one registry; the order
    /// and the origin of each path carry no meaning here.
    /// </param>
    public Dnd5eSystem(WorkspaceLayoutStore layoutStore, IReadOnlyList<string> packsPaths)
    {
        ArgumentNullException.ThrowIfNull(layoutStore);
        ArgumentNullException.ThrowIfNull(packsPaths);

        _layoutStore = layoutStore;

        Id = SystemId.Create(IdValue);
        ContentSetId = ContentId.Create(IdValue);

        var monster = Register<Monster>(MonsterTypeId, "Potwór", version: 1, (monster, picture) =>
        {
            var card = new MonsterCardView();
            card.SetMonster(monster, picture);
            return card;
        });
        var gear = Register<Gear>(GearTypeId, "Przedmiot", version: 1, (gear, picture) =>
        {
            var card = new GearCardView();
            card.SetGear(gear, picture);
            return card;
        });
        _contentTypes = new[] { monster, gear }.ToDictionary(type => type.Descriptor.Reference);

        _loadPacksStep = new LoadContentPacksStep(new ContentPackLoader(packsPaths, this));
        var warmCardsStep = new WarmContentCardsStep(() => _loadPacksStep.Registry, this);
        StartupSteps = [_loadPacksStep, warmCardsStep];

        (string Id, string IconResourceKey, ContentTabDefinition Definition)[] contentTabs =
        [
            ("dnd5e.monsters", "DungeonIconSkull", BuildMonsterContentTab(monster.Descriptor.Reference)),
            ("dnd5e.gear", "DungeonIconBackpack", BuildGearContentTab(gear.Descriptor.Reference)),
        ];
        ContentTabDefinitions = [.. contentTabs.Select(tab => tab.Definition)];
        SystemTabs =
        [
            .. contentTabs.Select(tab => new SystemTabDeclaration(
                tab.Id, tab.Definition.Title, tab.IconResourceKey, () => CreateContentTab(tab.Definition))),
        ];
        CampaignTabs = [new CampaignTabDeclaration("dnd5e.desk", "Biurko", "DungeonIconDockBottom", CreateDeskTabAsync)];
    }

    public SystemId Id { get; }

    /// <summary>
    /// The content-set id every <see cref="ContentTypeReference"/> and pack entry this system owns
    /// actually points at - distinct from <see cref="Id"/>, the frame's own identity for this system.
    /// Public, not internal: a system's own test project is a separate assembly with no reason to
    /// reference this one's internals, the same rationale <see cref="CampaignEntriesContext"/>'s
    /// public constructor already follows.
    /// </summary>
    public ContentId ContentSetId { get; }

    public string DisplayName => "Dungeons & Dragons 5e";

    public IReadOnlyList<SystemTabDeclaration> SystemTabs { get; }

    public IReadOnlyList<CampaignTabDeclaration> CampaignTabs { get; }

    /// <summary>
    /// This system's two content tabs - "Potwory" and "Przedmioty" - as data: a title and the content
    /// type profile(s) that fill it. <see cref="SystemTabs"/> builds each tab's view from one of these.
    /// </summary>
    public IReadOnlyList<ContentTabDefinition> ContentTabDefinitions { get; }

    public IReadOnlyList<StateModelDeclaration> StateModels { get; } = [InstancesModel.Declaration];

    public IReadOnlyList<IStartupStep> StartupSteps { get; }

    public bool HasSet(ContentId set) => set == ContentSetId;

    public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
    {
        if (_contentTypes.TryGetValue(reference, out var type))
        {
            descriptor = type.Descriptor;
            return true;
        }

        descriptor = default;
        return false;
    }

    public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
    {
        if (reference.Set != ContentSetId)
        {
            error = $"'{ContentSetId}' does not own content type reference '{reference}'.";
            return false;
        }

        if (!_contentTypes.TryGetValue(reference, out var type))
        {
            error = $"'{ContentSetId}' declares no content type '{reference.Type}'.";
            return false;
        }

        try
        {
            type.Validate(values);
        }
        catch (Exception ex)
        {
            // The deserializer is the validator: a missing required value, an unknown key, or a value of the wrong shape all surface as
            // an exception here, and its message is the only explanation the GM ever sees
            // (RegisteredEntry.UnresolvedDetail).
            error = ex.Message;
            return false;
        }

        error = null;
        return true;
    }

    public Control CreateCard(Entry entry, EntryPicture picture)
    {
        if (!_contentTypes.TryGetValue(entry.Type, out var type))
        {
            throw new InvalidOperationException($"'{ContentSetId}' cannot draw a card for content type reference '{entry.Type}'.");
        }

        return type.CreateCard(entry.Values, picture);
    }

    public IBrush? ResolveBadgeBrush(string colorKey) => RarityBrushes.GetValueOrDefault(colorKey);

    /// <summary>
    /// A rarity tier's own color, for the item card's rarity pill - the same one the list's badge
    /// resolves to; null for a rarity this system does not know.
    /// </summary>
    internal static IBrush? RarityBrush(string rarity) =>
        RarityColorKeys.TryGetValue(rarity, out var colorKey) ? RarityBrushes.GetValueOrDefault(colorKey) : null;

    /// <summary>
    /// One row of the content-type table. Every type declares "image" (<see cref="Monster.Image"/>,
    /// <see cref="Gear.Image"/>) as its picture, so the loader checks its path at load time without
    /// knowing what the type is, and every card stands a frame for it.
    /// </summary>
    private ContentTypeRegistration Register<TRecord>(
        string typeId, string name, int version, Func<TRecord, EntryPicture, Control> createCard) =>
        new(
            new ContentTypeDescriptor(new ContentTypeReference(ContentSetId, ContentId.Create(typeId)), name, version, ImagePropertyName),
            values => values.Read<TRecord>(),
            (values, picture) => createCard(values.Read<TRecord>(), picture));

    /// <summary>
    /// One of this system's content tabs, built fresh from its definition, never a stored instance: a
    /// System-category tab's factory runs again every time the GM opens it (warmup once, the real tab
    /// again on first click), and a <see cref="ContentTabViewModel"/> holds mutable list state that
    /// must not be shared between those two builds.
    /// </summary>
    private ITabContent CreateContentTab(ContentTabDefinition definition)
    {
        var viewModel = new ContentTabViewModel(_loadPacksStep.Registry, definition, AllContentTypes, this);
        return new DelegateTabContent(new ContentTabView { DataContext = viewModel });
    }

    /// <summary>
    /// Every content type reference every content tab this system declares names - what
    /// <see cref="ContentTabViewModel"/> needs to tell "no tab anywhere claims this broken entry"
    /// apart from "some other tab claims it" (see <c>ContentListModel</c>'s own remarks on
    /// <c>allKnownTypes</c>). Gathered once here, from the same definitions every tab is built from - never per tab, which would make each tab think
    /// it was the only one that existed.
    /// </summary>
    private IReadOnlyCollection<ContentTypeReference> AllContentTypes =>
        ContentTabDefinitions.SelectMany(tab => tab.ContentTypes).Select(profile => profile.Type).Distinct().ToArray();

    /// <summary>
    /// "Potwory": category "Grupa" = <see cref="Monster.Group"/>, never <see cref="Monster.Type"/> -
    /// a monster with no declared group has no category; tags = size, type, alignment; badge =
    /// challenge, no color key; two value filters - "Typ" (<see cref="Monster.Type"/>, alphabetical)
    /// before "Wyzwanie" (<see cref="ChallengeOrder"/>) - and one sort, "Wyzwanie".
    /// </summary>
    private static ContentTabDefinition BuildMonsterContentTab(ContentTypeReference monster)
    {
        var profile = new ContentTypeProfile<Monster>(
            monster,
            category: new ContentCategorySpec<Monster>("Grupa", monster => monster.Group),
            tags: monster => [monster.Size, monster.Type, monster.Alignment],
            badge: monster => new ContentBadge(monster.Challenge),
            valueFilters:
            [
                new ContentValueFilterSpec<Monster>("Typ", monster => monster.Type, PolishAlphabeticalOrder),
                new ContentValueFilterSpec<Monster>("Wyzwanie", monster => monster.Challenge, ChallengeOrder),
            ],
            sorts: [new ContentSortSpec<Monster>("Wyzwanie", (a, b) => ChallengeOrder.Compare(a.Challenge, b.Challenge))]);

        return new ContentTabDefinition("Potwory", [profile], "Żadna paczka nie ma jeszcze potworów.");
    }

    /// <summary>
    /// "Przedmioty": category "Kategoria" = <see cref="Gear.Category"/> - the breadcrumb's middle
    /// segment and the tab's category filter, like a monster's group; tags = "magiczny" for a magical
    /// item, then the subtype, each only when there is one (the card puts the rarity pill before them);
    /// badge = rarity, with a color key per <see cref="RarityColorKeys"/> for a recognised tier and
    /// none for anything else; three value filters - "Rzadkość" (<see cref="RarityOrder"/>), "Magia"
    /// ("magiczny" / "niemagiczny") and "Dostrojenie" ("wymaga" / "nie wymaga") - and two sorts:
    /// "Rzadkość" in the same <see cref="RarityOrder"/>, and "Wartość" by number (every item has one).
    /// </summary>
    private static ContentTabDefinition BuildGearContentTab(ContentTypeReference gear)
    {
        var profile = new ContentTypeProfile<Gear>(
            gear,
            category: new ContentCategorySpec<Gear>("Kategoria", gear => gear.Category),
            tags: gear => [.. new[] { gear.Magical ? MagicalTag : null, gear.Subtype }.OfType<string>()],
            badge: gear => new ContentBadge(gear.Rarity, RarityColorKeys.GetValueOrDefault(gear.Rarity)),
            valueFilters:
            [
                new ContentValueFilterSpec<Gear>("Rzadkość", gear => gear.Rarity, RarityOrder),
                new ContentValueFilterSpec<Gear>("Magia", gear => gear.Magical ? MagicalTag : Mundane, MagicOrder),
                new ContentValueFilterSpec<Gear>("Dostrojenie", gear => gear.Attunement ? NeedsAttunement : NeedsNoAttunement, AttunementOrder),
            ],
            sorts:
            [
                new ContentSortSpec<Gear>("Rzadkość", (a, b) => RarityOrder.Compare(a.Rarity, b.Rarity)),
                new ContentSortSpec<Gear>("Wartość", (a, b) => a.Value.CompareTo(b.Value)),
            ]);

        return new ContentTabDefinition("Przedmioty", [profile], "Żadna paczka nie ma jeszcze przedmiotów.");
    }

    /// <summary>
    /// The "Biurko" Campaign-category tab: the shared desk (<see cref="CampaignDesk"/>), stocked with
    /// this system's own tool belt and nothing else. Building the tool list is this system's own job,
    /// from a <see cref="CampaignEntriesContext"/> it builds itself out of the tab context plus its
    /// own registry and type catalog.
    /// </summary>
    private async Task<ITabContent> CreateDeskTabAsync(CampaignTabContext context)
    {
        var toolContext = new CampaignEntriesContext(context, _loadPacksStep.Registry, this);
        var tools = BuildTools(toolContext);

        return await CampaignDesk.CreateAsync(context, _layoutStore, tools);
    }

    /// <summary>
    /// This system's tool belt: one window, "Świat kampanii", listing this campaign's instances and
    /// offering this system's own resolved entries to bring in as new ones. Sized from the
    /// shell's shared desk-tool-window tokens (<see cref="WorkspaceGridSettings"/>) - no numbers
    /// invented here.
    /// </summary>
    private IReadOnlyList<WorkspacePanelDescriptor> BuildTools(CampaignEntriesContext context)
    {
        var minimum = WorkspaceMetrics.Fallback;
        var minWidth = Math.Max(minimum.MinPanelWidth, WorkspaceGridSettings.ToolPanelMinWidth);
        var minHeight = Math.Max(minimum.MinPanelHeight, WorkspaceGridSettings.ToolPanelMinHeight);

        return
        [
            new WorkspacePanelDescriptor(
                "dnd5e.instances",
                "Świat kampanii",
                "DungeonIconUsers",
                WorkspacePanelGroup.World,
                new PanelPlacement(
                    WorkspaceGridSettings.CellSize,
                    WorkspaceGridSettings.CellSize,
                    minWidth,
                    minHeight),
                new PanelConstraints(
                    minWidth,
                    minHeight,
                    WorkspaceGridSettings.ToolPanelMaxWidth,
                    WorkspaceGridSettings.ToolPanelMaxHeight),
                () => BuildToolView(context)),
        ];
    }

    private Control BuildToolView(CampaignEntriesContext context) =>
        new CampaignInstancesToolView
        {
            DataContext = new CampaignInstancesToolViewModel(context, ContentSetId),
        };

    /// <summary>
    /// A content type this system declares. <see cref="Validate"/> and <see cref="CreateCard"/> both
    /// read the entry's values into the type's own record; reading is the whole validation.
    /// </summary>
    private sealed record ContentTypeRegistration(
        ContentTypeDescriptor Descriptor,
        Action<ContentValues> Validate,
        Func<ContentValues, EntryPicture, Control> CreateCard);
}
