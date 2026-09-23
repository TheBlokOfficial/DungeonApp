using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using DungeonApp.Core.Content;
using DungeonApp.Core.Content.Instances;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Content;
using DungeonApp.Library.Entries.Desktop.Content;
using DungeonApp.Library.Entries.Desktop.Features.Registry;
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
/// </summary>
public sealed class Dnd5eSystem : IGameSystem
{
    // Internal, not private: InstanceRowViewModel's hit-point editing needs the same id to decide
    // whether a row is a monster, and docs/decisions.md permits branching on this id only inside
    // this system - duplicating the literal there instead would let the two silently drift.
    internal const string MonsterTypeId = "monster";
    private const string GearTypeId = "gear";

    private readonly WorkspaceLayoutStore _layoutStore;
    private readonly ContentTypeDescriptor _monster;
    private readonly ContentTypeDescriptor _gear;

    public Dnd5eSystem(WorkspaceLayoutStore layoutStore)
    {
        ArgumentNullException.ThrowIfNull(layoutStore);

        _layoutStore = layoutStore;

        Id = ContentId.Create("dnd5e");
        _monster = new ContentTypeDescriptor(new ContentTypeReference(Id, ContentId.Create(MonsterTypeId)), "Potwór", 1);
        _gear = new ContentTypeDescriptor(new ContentTypeReference(Id, ContentId.Create(GearTypeId)), "Przedmiot", 1);

        SystemTabs = [new SystemTabDeclaration("dnd5e.registry", "Rejestr", "DungeonIconDatabase", CreateRegistryTab)];
        CampaignTabs = [new CampaignTabDeclaration("dnd5e.desk", "Biurko", "DungeonIconDockBottom", CreateDeskTabAsync)];
    }

    public ContentId Id { get; }

    public string DisplayName => "Dungeons & Dragons 5e";

    public IReadOnlyList<SystemTabDeclaration> SystemTabs { get; }

    public IReadOnlyList<CampaignTabDeclaration> CampaignTabs { get; }

    public IReadOnlyList<StateModelDeclaration> StateModels { get; } = [InstancesModel.Declaration];

    public bool HasSet(ContentId set) => set == Id;

    public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
    {
        if (reference.Set == Id && reference.Type.Value == MonsterTypeId)
        {
            descriptor = _monster;
            return true;
        }

        if (reference.Set == Id && reference.Type.Value == GearTypeId)
        {
            descriptor = _gear;
            return true;
        }

        descriptor = default;
        return false;
    }

    public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
    {
        if (reference.Set != Id)
        {
            error = $"'{Id}' does not own content type reference '{reference}'.";
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
                    error = $"'{Id}' declares no content type '{reference.Type}'.";
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
        if (entry.Type.Set == Id && entry.Type.Type.Value == MonsterTypeId)
        {
            var view = new MonsterCardView();
            view.SetMonster(entry.Values.Read<Monster>());
            return view;
        }

        if (entry.Type.Set == Id && entry.Type.Type.Value == GearTypeId)
        {
            var view = new GearCardView();
            view.SetGear(entry.Values.Read<Gear>());
            return view;
        }

        throw new InvalidOperationException($"'{Id}' cannot draw a card for content type reference '{entry.Type}'.");
    }

    /// <summary>The "Rejestr" System-category tab: today's registry screen, drawn by this system's own presentation.</summary>
    private ITabContent CreateRegistryTab(SystemTabContext context)
    {
        var viewModel = new RegistryViewModel(context.Registry, this);
        return new DelegateTabContent(new RegistryView { DataContext = viewModel });
    }

    /// <summary>
    /// The "Biurko" Campaign-category tab: the shared desk (<see cref="CampaignDesk"/>), stocked with
    /// this system's own tool belt and nothing else - there is no cross-system tool provider
    /// stitching several systems' tools together any more, so building the tool list is this
    /// system's own job now, from a <see cref="CampaignEntriesContext"/> it builds itself out of the
    /// tab context plus its own type catalog.
    /// </summary>
    private async Task<ITabContent> CreateDeskTabAsync(CampaignTabContext context)
    {
        var toolContext = new CampaignEntriesContext(context, this);
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
            DataContext = new CampaignInstancesToolViewModel(context, Id),
        };
}
