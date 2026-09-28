using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using DungeonApp.Library.Entries;
using DungeonApp.Library.Entries.Instances;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Startup;
using DungeonApp.Library.Entries.Desktop.Content;
using DungeonApp.Library.Entries.Desktop.Features.ContentTab;
using DungeonApp.Library.Entries.Desktop.Startup;
using DungeonApp.Library.Workspace.Controls.Workspace;
using DungeonApp.Library.Workspace.Features.CampaignWorkspace;
using DungeonApp.Library.Workspace.Features.CampaignWorkspace.Layout;
using DungeonApp.Library.Workspace.Features.CampaignWorkspace.Panels;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The one place in the application allowed to know what a monster or a piece of gear is. Declares
/// two content types - <c>monster</c> ("Potwór", version 1) and <c>gear</c> ("Przedmiot", version 1)
/// - builds their cards, and declares this system's tabs: "Potwory" and "Przedmioty" (built from the
/// library's content-tab skeleton, krok 10 zlecenie 2) in the System category, "Biurko" in the
/// Campaign category.
/// <para>
/// The dispatch on a content type's id inside <see cref="TryGet"/>, <see cref="TryValidate"/> and
/// <see cref="CreateCard"/> below is legal and necessary here: docs/architecture.md's "Kontrakty są
/// interfejsami" section names exactly one place allowed to be concrete about what a monster is, and
/// this is it. The ban that section and <c>CoreEntryKindIndependenceTests</c> enforce is on
/// <c>Core</c> or <c>Desktop</c> branching on entry kind - neither of them contains the word
/// "monster" anywhere, and neither ever will just because this switch exists.
/// </para>
/// <para>
/// Carries two distinct identities on purpose (docs/architecture.md, "Rama, biblioteka, system"):
/// <see cref="Id"/> is what the frame knows this system as (<see cref="SystemId"/>, never
/// <c>DungeonApp.Library.Entries</c>'s own <see cref="ContentId"/>); <see cref="ContentSetId"/> is the
/// content-set id every content type reference and pack entry in this system actually points at. Both
/// are minted from the same literal, but nothing enforces that they stay equal - a system is free to
/// pick a different one for either.
/// </para>
/// </summary>
public sealed class Dnd5eSystem : IGameSystem, IContentTypeCatalog, IContentPresentation
{
    // Internal, not private: InstanceRowViewModel's hit-point editing needs the same id to decide
    // whether a row is a monster, and docs/decisions.md permits branching on this id only inside
    // this system - duplicating the literal there instead would let the two silently drift.
    internal const string MonsterTypeId = "monster";
    private const string GearTypeId = "gear";

    /// <summary>The JSON name of <see cref="Monster.Image"/> and <see cref="Gear.Image"/>.</summary>
    private const string ImagePropertyName = "image";

    // Public, not internal: DungeonApp.App/Program.cs - the one place allowed to name a system by
    // name - is a separate assembly, and reads this to compute this system's packs directory from
    // its own identifier (docs/architecture.md, "Gdzie mieszka stan":
    // "Dokumenty\DungeonApp\<system>\packs\"), rather than typing the literal "dnd5e" a second time
    // somewhere the two could drift apart.
    public const string IdValue = "dnd5e";

    /// <summary>
    /// Rarity tiers in display order, paired with the color-key name (krok 10, zlecenie 1, część C)
    /// their badge carries - an opaque intent for <see cref="ContentBadge.ColorKey"/>, not a color;
    /// zlecenie 2 is the only place allowed to turn it into one. A rarity string this system does not
    /// recognise gets no color key and falls after every known tier when "Rzadkość" is ordered
    /// (see <see cref="RankedTextComparer"/>).
    /// </summary>
    private static readonly IReadOnlyList<(string Tier, string ColorKey)> RarityTiers =
    [
        ("Pospolity", "rarity-common"),
        ("Niezwykły", "rarity-uncommon"),
        ("Rzadki", "rarity-rare"),
        ("Bardzo rzadki", "rarity-very-rare"),
        ("Legendarny", "rarity-legendary"),
        ("Artefakt", "rarity-artifact"),
    ];

    private static readonly IComparer<string> RarityOrder = new RankedTextComparer([.. RarityTiers.Select(tier => tier.Tier)]);

    private static readonly IReadOnlyDictionary<string, string> RarityColorKeys =
        RarityTiers.ToDictionary(tier => tier.Tier, tier => tier.ColorKey, StringComparer.Ordinal);

    /// <summary>
    /// The brush each rarity color key resolves to (<see cref="ResolveBadgeBrush"/>) - this system's
    /// own colors, minted fresh rather than borrowed from the frame's meaning-carrying tokens
    /// (docs/architecture.md, "Niezmiennik interfejsu": "Skale należące do systemu... mają własne
    /// kolory w systemie i nie pożyczają kolorów znaczeń z motywu ramy"). Hues follow the usual
    /// rarity convention (gray, green, blue, violet, orange, pink) and none is the accent color.
    /// Each is bright enough for its text to keep at least 4.5:1 against its own dimmed pill
    /// (the same color at 16%) laid on the list row at rest, under the pointer and selected
    /// (krok 10, brief 3a - the ratios are in that brief's report).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IBrush> RarityBrushes = new Dictionary<string, IBrush>(StringComparer.Ordinal)
    {
        // Rzadkość: pospolity.
        ["rarity-common"] = new SolidColorBrush(Color.Parse("#A9A8A1")),
        // Rzadkość: niezwykły.
        ["rarity-uncommon"] = new SolidColorBrush(Color.Parse("#95BA9C")),
        // Rzadkość: rzadki.
        ["rarity-rare"] = new SolidColorBrush(Color.Parse("#7AAAD6")),
        // Rzadkość: bardzo rzadki.
        ["rarity-very-rare"] = new SolidColorBrush(Color.Parse("#BA9EDC")),
        // Rzadkość: legendarny.
        ["rarity-legendary"] = new SolidColorBrush(Color.Parse("#E8A060")),
        // Rzadkość: artefakt.
        ["rarity-artifact"] = new SolidColorBrush(Color.Parse("#E88AB6")),
    };

    private static readonly IComparer<string> ChallengeOrder = new ChallengeOrderComparer();

    /// <summary>The "Typ" filter's option order: alphabetical, Polish collation, case insensitive.</summary>
    private static readonly IComparer<string> PolishAlphabeticalOrder =
        StringComparer.Create(new System.Globalization.CultureInfo("pl-PL"), ignoreCase: true);

    private readonly WorkspaceLayoutStore _layoutStore;
    private readonly ContentTypeDescriptor _monster;
    private readonly ContentTypeDescriptor _gear;
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

        Id = SystemId.Create("dnd5e");
        ContentSetId = ContentId.Create("dnd5e");
        // Both types declare "image" (Monster.Image, Gear.Image) as their picture, so the loader
        // checks its path at load time without knowing what either type is.
        _monster = new ContentTypeDescriptor(new ContentTypeReference(ContentSetId, ContentId.Create(MonsterTypeId)), "Potwór", 1, ImagePropertyName);
        _gear = new ContentTypeDescriptor(new ContentTypeReference(ContentSetId, ContentId.Create(GearTypeId)), "Przedmiot", 1, ImagePropertyName);

        _loadPacksStep = new LoadContentPacksStep(new ContentPackLoader(packsPaths, this));
        var warmCardsStep = new WarmContentCardsStep(() => _loadPacksStep.Registry, this);
        StartupSteps = [_loadPacksStep, warmCardsStep];

        ContentTabDefinitions = [BuildMonsterContentTab(), BuildGearContentTab()];

        // Icon choice (krok 10, brief B2): purpose-drawn icons replace the krok 10 zlecenie 2 picks -
        // DungeonIconDragon (a beast's head) for "Potwory", DungeonIconBackpack for "Przedmioty".
        // DungeonIconBookOpen stays in Icons.axaml (GlobalSidebarViewModel's own shelf icon still
        // uses it); DungeonIconBoxes had no other user and was removed with it.
        SystemTabs =
        [
            new SystemTabDeclaration("dnd5e.monsters", "Potwory", "DungeonIconDragon", () => CreateContentTab(0)),
            new SystemTabDeclaration("dnd5e.gear", "Przedmioty", "DungeonIconBackpack", () => CreateContentTab(1)),
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
    /// This system's two content tabs - "Potwory" and "Przedmioty" - as pure data: a title and the
    /// content type profile(s) that fill it (krok 10, zlecenie 1, część C). Not stood up on the
    /// System bar and not touched by <see cref="SystemTabs"/> or <see cref="CreateRegistryTab"/> -
    /// building the actual tab, from <c>ContentListModel</c>, is zlecenie 2.
    /// </summary>
    public IReadOnlyList<ContentTabDefinition> ContentTabDefinitions { get; }

    public IReadOnlyList<StateModelDeclaration> StateModels { get; } = [InstancesModel.Declaration];

    public IReadOnlyList<IStartupStep> StartupSteps { get; }

    public bool HasSet(ContentId set) => set == ContentSetId;

    public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
    {
        if (reference.Set == ContentSetId && reference.Type.Value == MonsterTypeId)
        {
            descriptor = _monster;
            return true;
        }

        if (reference.Set == ContentSetId && reference.Type.Value == GearTypeId)
        {
            descriptor = _gear;
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

        try
        {
            switch (reference.Type.Value)
            {
                case MonsterTypeId:
                    values.Read<Monster>();
                    break;

                case GearTypeId:
                    values.Read<Gear>();
                    break;

                default:
                    error = $"'{ContentSetId}' declares no content type '{reference.Type}'.";
                    return false;
            }
        }
        catch (Exception ex)
        {
            // The deserializer is the validator (docs/architecture.md, "Deklaracja treści"): a
            // missing required value, an unknown key, or a value of the wrong shape all surface as
            // an exception here, and its message is the only explanation the GM ever sees
            // (RegisteredEntry.UnresolvedDetail).
            error = ex.Message;
            return false;
        }

        error = null;
        return true;
    }

    public Control CreateCard(Entry entry)
    {
        if (entry.Type.Set == ContentSetId && entry.Type.Type.Value == MonsterTypeId)
        {
            var view = new MonsterCardView();
            view.SetMonster(entry.Values.Read<Monster>());
            return view;
        }

        if (entry.Type.Set == ContentSetId && entry.Type.Type.Value == GearTypeId)
        {
            var view = new GearCardView();
            view.SetGear(entry.Values.Read<Gear>());
            return view;
        }

        throw new InvalidOperationException($"'{ContentSetId}' cannot draw a card for content type reference '{entry.Type}'.");
    }

    public IBrush? ResolveBadgeBrush(string colorKey) => RarityBrushes.GetValueOrDefault(colorKey);

    /// <summary>
    /// One of this system's two content tabs, built fresh from the library's content-tab skeleton
    /// (krok 10, zlecenie 2) - <see cref="ContentTabDefinitions"/>'s own index, never a stored
    /// instance: a System-category tab's factory runs again every time the GM opens it (warmup once,
    /// the real tab again on first click), and a <see cref="ContentTabViewModel"/> holds mutable
    /// list state that must not be shared between those two builds.
    /// </summary>
    private ITabContent CreateContentTab(int definitionIndex)
    {
        var definition = ContentTabDefinitions[definitionIndex];
        var viewModel = new ContentTabViewModel(_loadPacksStep.Registry, definition, AllContentTypes, this);
        return new DelegateTabContent(new ContentTabView { DataContext = viewModel });
    }

    /// <summary>
    /// Every content type reference every content tab this system declares names - what
    /// <see cref="ContentTabViewModel"/> needs to tell "no tab anywhere claims this broken entry"
    /// apart from "some other tab claims it" (docs/architecture.md, "Zakładki treści"; see
    /// <c>ContentListModel</c>'s own remarks on <c>allKnownTypes</c>). Gathered once here, from the
    /// same two definitions every tab is built from - never per tab, which would make each tab think
    /// it was the only one that existed.
    /// </summary>
    private IReadOnlyCollection<ContentTypeReference> AllContentTypes =>
        ContentTabDefinitions.SelectMany(tab => tab.ContentTypes).Select(profile => profile.Type).Distinct().ToArray();

    /// <summary>
    /// "Potwory": category "Grupa" = <see cref="Monster.Group"/> (krok 10, brief A9 - never
    /// <see cref="Monster.Type"/>; a monster with no declared group has no category, full stop);
    /// tags = size, type, alignment; badge = challenge, no color key; two value filters - "Typ"
    /// (<see cref="Monster.Type"/>, alphabetical) before "Wyzwanie" (<see cref="ChallengeOrder"/>) -
    /// and one sort, "Wyzwanie" (krok 10, zlecenie 1, część C; porcja 3 zakładek treści).
    /// </summary>
    private ContentTabDefinition BuildMonsterContentTab()
    {
        var profile = new ContentTypeProfile<Monster>(
            _monster.Reference,
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
    /// "Przedmioty": no category (the tab has no category filter); no tags
    /// (krok 10, brief 3a); badge = rarity, with a color key per <see cref="RarityColorKeys"/> for a recognised
    /// tier and none for anything else; one value filter, "Rzadkość", ordered by
    /// <see cref="RarityOrder"/>; one sort, "Rzadkość", in the same <see cref="RarityOrder"/>
    /// (porcja 3c zakładek treści).
    /// </summary>
    private ContentTabDefinition BuildGearContentTab()
    {
        var profile = new ContentTypeProfile<Gear>(
            _gear.Reference,
            category: null,
            tags: gear => [],
            badge: gear => new ContentBadge(gear.Rarity, RarityColorKeys.GetValueOrDefault(gear.Rarity)),
            valueFilters: [new ContentValueFilterSpec<Gear>("Rzadkość", gear => gear.Rarity, RarityOrder)],
            sorts: [new ContentSortSpec<Gear>("Rzadkość", (a, b) => RarityOrder.Compare(a.Rarity, b.Rarity))]);

        return new ContentTabDefinition("Przedmioty", [profile], "Żadna paczka nie ma jeszcze przedmiotów.");
    }

    /// <summary>
    /// The "Biurko" Campaign-category tab: the shared desk (<see cref="CampaignDesk"/>), stocked with
    /// this system's own tool belt and nothing else - there is no cross-system tool provider
    /// stitching several systems' tools together any more, so building the tool list is this
    /// system's own job now, from a <see cref="CampaignEntriesContext"/> it builds itself out of the
    /// tab context plus its own registry and type catalog.
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
}
