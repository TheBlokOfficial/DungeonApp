using System;
using System.Collections.Generic;
using Avalonia.Controls;
using DungeonApp.Core.Entries;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Workspace.Panels;

namespace DungeonApp.Desktop.Tests.Entries;

/// <summary>
/// A minimal <see cref="IGameSystem"/> for the tests that need an <see cref="IContentTypeCatalog"/>
/// and an <see cref="IContentPresentation"/>. It knows exactly the descriptors it is given and draws a
/// trivial placeholder card for any resolved entry - it exercises the content plumbing, not any real
/// content type, so it names none.
/// <para>
/// Implements <see cref="IContentTypeCatalog"/> and <see cref="IContentPresentation"/> directly, not
/// through <see cref="IGameSystem"/>, because the frame's own contract carries neither; so
/// <see cref="ContentSetId"/> is this fake's own concrete identity, separate from the frame's
/// <see cref="Id"/>, the same split <c>Dnd5eSystem</c> makes.
/// </para>
/// </summary>
internal sealed class FakeContentSystem(
    ContentId id,
    IReadOnlyList<ContentTypeDescriptor> descriptors,
    Func<ContentValues, string?>? validate = null,
    IReadOnlyList<SystemTabDeclaration>? systemTabs = null,
    IReadOnlyList<CampaignTabDeclaration>? campaignTabs = null) : IGameSystem, IContentTypeCatalog, IContentPresentation
{
    public SystemId Id { get; } = SystemId.Create(id.Value);

    public ContentId ContentSetId { get; } = id;

    public string DisplayName { get; } = id.ToString();

    public IReadOnlyList<SystemTabDeclaration> SystemTabs { get; } = systemTabs ?? [];

    public IReadOnlyList<CampaignTabDeclaration> CampaignTabs { get; } = campaignTabs ?? [];

    public IReadOnlyList<WorkspacePanelDescriptor> CreateDeskTools(CampaignTabContext context) => [];

    public IReadOnlyList<StateModelDeclaration> StateModels { get; } = [];

    public IReadOnlyList<DungeonApp.Desktop.Systems.WorldEntityType> EntityTypes { get; } = [];

    public WorldCatalogSource GetWorldCatalogSource() => WorldCatalogSource.Empty;

    public string? RowHint(DungeonApp.Core.Entries.ResolvedEntity entity) => null;

    public IReadOnlyList<IStartupStep> StartupSteps { get; } = [];

    public bool HasSet(ContentId set) => set == ContentSetId;

    public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
    {
        foreach (var candidate in descriptors)
        {
            if (candidate.Reference == reference)
            {
                descriptor = candidate;
                return true;
            }
        }

        descriptor = default;
        return false;
    }

    public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
    {
        if (!TryGet(reference, out _))
        {
            error = "unknown content type.";
            return false;
        }

        error = validate?.Invoke(values);
        return error is null;
    }

    public Control CreateCard(Entry entry, EntryPicture picture) => new TextBlock { Text = entry.Name };

    public Avalonia.Media.IBrush? ResolveBadgeBrush(string colorKey) => null;
}
