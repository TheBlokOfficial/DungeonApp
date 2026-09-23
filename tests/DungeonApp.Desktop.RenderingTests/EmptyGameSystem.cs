using System.Collections.Generic;
using Avalonia.Controls;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Startup;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The one system <see cref="TestAppBuilder"/> boots this assembly's <see cref="App"/> with - it only
/// has to satisfy <c>App.Initialize()</c>'s "at least one system" guard so the real application
/// styles load. No test in this project reads its tabs or content types; each test builds its own
/// <c>GlobalSidebarViewModel</c> directly with its own declarations instead.
/// </summary>
internal sealed class EmptyGameSystem : IGameSystem
{
    public SystemId Id { get; } = SystemId.Create("rendering-tests-empty");

    public string DisplayName => "Rendering Tests";

    public IReadOnlyList<SystemTabDeclaration> SystemTabs { get; } = [];

    public IReadOnlyList<CampaignTabDeclaration> CampaignTabs { get; } = [];

    public IReadOnlyList<StateModelDeclaration> StateModels { get; } = [];

    public IReadOnlyList<IStartupStep> StartupSteps { get; } = [];
}

/// <summary>A minimal <see cref="ITabContent"/> for tab declarations that this project's tests never click.</summary>
internal sealed class FakeTabContent : ITabContent
{
    public Control Content { get; } = new Panel();

    public void Dispose()
    {
    }
}
