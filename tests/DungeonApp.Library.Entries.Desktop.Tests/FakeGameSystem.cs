using System;
using System.Collections.Generic;
using Avalonia.Controls;
using DungeonApp.Core.Content;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Startup;
using DungeonApp.Library.Entries.Desktop.Content;

namespace DungeonApp.Library.Entries.Desktop.Tests;

/// <summary>
/// A minimal <see cref="IGameSystem"/> for the Library.Entries.Desktop tests that need an
/// <see cref="IContentPresentation"/> to hand a <see cref="Features.Registry.RegistryViewModel"/>
/// (<c>RegistryViewModelTests</c>, <c>LoadContentPacksStepTests</c>). It knows exactly the descriptors
/// it is given and draws a trivial placeholder card for any resolved entry - it exists to exercise the
/// registry's plumbing, not to stand in for any real content type, so it names none.
/// <para>
/// Implements <see cref="IContentTypeCatalog"/> and <see cref="IContentPresentation"/> directly, not
/// through <see cref="IGameSystem"/> - the frame's own contract no longer carries either
/// (docs/architecture.md, "Rama, biblioteka, system"), so <see cref="ContentSetId"/> is this fake's own
/// concrete identity, separate from the frame's <see cref="Id"/>, the same split
/// <c>Dnd5eSystem</c> makes.
/// </para>
/// <para>
/// Copied minimally from <c>DungeonApp.Desktop.Tests.FakeGameSystem</c> rather than shared (docs/tasks.md,
/// etap 2): that class is <c>internal</c> and still backs several Desktop.Tests fixtures unrelated to
/// this move, and a reference between two test projects would be a second, needless edge for the sake
/// of one fake.
/// </para>
/// </summary>
internal sealed class FakeGameSystem(
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

    public IReadOnlyList<StateModelDeclaration> StateModels { get; } = [];

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

    public Control CreateCard(Entry entry) => new TextBlock { Text = entry.Name };
}
