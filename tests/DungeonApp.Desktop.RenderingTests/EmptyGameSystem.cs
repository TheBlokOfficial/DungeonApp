using System.Collections.Generic;
using Avalonia.Controls;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Content;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The one system <see cref="TestAppBuilder"/> boots this assembly's <see cref="App"/> with - it only
/// has to satisfy <c>App.Initialize()</c>'s "at least one system" guard so the real application
/// styles load. No test in this project reads its tabs or content types; each test builds its own
/// <c>GlobalSidebarViewModel</c> directly with its own declarations instead.
/// </summary>
internal sealed class EmptyGameSystem : IGameSystem
{
    public ContentId Id { get; } = ContentId.Create("rendering-tests-empty");

    public string DisplayName => "Rendering Tests";

    public IReadOnlyList<SystemTabDeclaration> SystemTabs { get; } = [];

    public IReadOnlyList<CampaignTabDeclaration> CampaignTabs { get; } = [];

    public bool HasSet(ContentId set) => set == Id;

    public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
    {
        descriptor = default;
        return false;
    }

    public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
    {
        error = "unknown content type.";
        return false;
    }

    public Control CreateCard(Entry entry) => new TextBlock { Text = entry.Name };
}

/// <summary>A minimal <see cref="ITabContent"/> for tab declarations that this project's tests never click.</summary>
internal sealed class FakeTabContent : ITabContent
{
    public Control Content { get; } = new Panel();

    public void Dispose()
    {
    }
}
