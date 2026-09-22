using System;
using System.Collections.Generic;
using Avalonia.Controls;
using DungeonApp.Core.Content;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Content;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// A minimal <see cref="IGameSystem"/> shared by every Desktop test that needs one:
/// <see cref="ContentPackLoader"/> tests use it as an <see cref="IContentTypeCatalog"/>, registry
/// tests use it as an <see cref="IContentPresentation"/> too, and the tab-lifecycle tests
/// (<c>ActiveSystemSessionTests</c>) use its <see cref="SystemTabs"/> and <see cref="CampaignTabs"/>.
/// It knows exactly the descriptors it is given and draws a trivial placeholder card for any resolved
/// entry - it exists to exercise the shell's plumbing, not to stand in for any real content type, so
/// it names none.
/// <para>
/// <paramref name="validate"/> lets a test simulate <see cref="EntryUnresolvedReason.ValuesRejected"/>
/// without this fake ever knowing what a real content type's values look like: return an error
/// message to reject, or <see langword="null"/> to accept. Defaults to always accepting.
/// </para>
/// <para>
/// <paramref name="systemTabs"/> and <paramref name="campaignTabs"/> default to empty, so a test that
/// only needs a catalog or a presentation is not forced to think about tabs at all.
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
