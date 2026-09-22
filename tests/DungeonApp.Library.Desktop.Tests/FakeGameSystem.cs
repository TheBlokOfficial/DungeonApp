using System;
using System.Collections.Generic;
using Avalonia.Controls;
using DungeonApp.Core.Content;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Content;

namespace DungeonApp.Library.Desktop.Tests;

/// <summary>
/// A minimal <see cref="IGameSystem"/> for the Library.Desktop tests that need an
/// <see cref="IContentPresentation"/> to hand a <see cref="Features.Registry.RegistryViewModel"/>
/// (<c>RegistryViewModelTests</c>). It knows exactly the descriptors it is given and draws a trivial
/// placeholder card for any resolved entry - it exists to exercise the registry's plumbing, not to
/// stand in for any real content type, so it names none.
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
    IReadOnlyList<CampaignTabDeclaration>? campaignTabs = null) : IGameSystem
{
    public ContentId Id { get; } = id;

    public string DisplayName { get; } = id.ToString();

    public IReadOnlyList<SystemTabDeclaration> SystemTabs { get; } = systemTabs ?? [];

    public IReadOnlyList<CampaignTabDeclaration> CampaignTabs { get; } = campaignTabs ?? [];

    public IReadOnlyList<StateModelDeclaration> StateModels { get; } = [];

    public bool HasSet(ContentId set) => set == Id;

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
