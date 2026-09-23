using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using DungeonApp.Library.Entries;
using DungeonApp.Library.Entries.Instances;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Startup;
using DungeonApp.Library.Entries.Desktop.Content;
using DungeonApp.Library.Entries.Desktop.Features.Registry;
using DungeonApp.Library.Entries.Desktop.Startup;
using DungeonApp.Library.Workspace.Controls.Workspace;
using DungeonApp.Library.Workspace.Features.CampaignWorkspace;
using DungeonApp.Library.Workspace.Features.CampaignWorkspace.Layout;
using DungeonApp.Library.Workspace.Features.CampaignWorkspace.Panels;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The one place in the application allowed to know what a monster or a piece of gear is. Declares
/// two content types - <c>monster</c> ("Potwór", version 1) and <c>gear</c> ("Przedmiot", version 1)
/// - builds their cards, and declares this system's tabs: "Rejestr" in the System category, "Biurko"
/// in the Campaign category.
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

    private readonly WorkspaceLayoutStore _layoutStore;
    private readonly ContentTypeDescriptor _monster;
    private readonly ContentTypeDescriptor _gear;
    private readonly LoadContentPacksStep _loadPacksStep;

    public Dnd5eSystem(WorkspaceLayoutStore layoutStore, string packsPath)
    {
        ArgumentNullException.ThrowIfNull(layoutStore);
        ArgumentNullException.ThrowIfNull(packsPath);

        _layoutStore = layoutStore;

        Id = SystemId.Create("dnd5e");
        ContentSetId = ContentId.Create("dnd5e");
        _monster = new ContentTypeDescriptor(new ContentTypeReference(ContentSetId, ContentId.Create(MonsterTypeId)), "Potwór", 1);
        _gear = new ContentTypeDescriptor(new ContentTypeReference(ContentSetId, ContentId.Create(GearTypeId)), "Przedmiot", 1);

        _loadPacksStep = new LoadContentPacksStep(new ContentPackLoader(packsPath, this));
        var warmCardsStep = new WarmContentCardsStep(() => _loadPacksStep.Registry, this);
        StartupSteps = [_loadPacksStep, warmCardsStep];

        SystemTabs = [new SystemTabDeclaration("dnd5e.registry", "Rejestr", "DungeonIconDatabase", CreateRegistryTab)];
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

    /// <summary>The "Rejestr" System-category tab: today's registry screen, drawn by this system's own presentation.</summary>
    private ITabContent CreateRegistryTab()
    {
        var viewModel = new RegistryViewModel(_loadPacksStep.Registry, this);
        return new DelegateTabContent(new RegistryView { DataContext = viewModel });
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
