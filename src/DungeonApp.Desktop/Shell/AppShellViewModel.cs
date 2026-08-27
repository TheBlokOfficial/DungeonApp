using DungeonApp.Desktop.Features.CampaignWorkspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.Shell.StatusBar;
using DungeonApp.Desktop.Shell.TopBar;
using DungeonApp.Desktop.Shell.Workspace;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell;

public sealed class AppShellViewModel : ObservableObject
{
    private const string CampaignsSectionId = "campaigns";

    private readonly CampaignWorkspaceViewModel _campaignWorkspace;

    private object _currentWorkspaceContent;

    public AppShellViewModel(WorkspaceLayoutStore layoutStore)
    {
        TopBar = new TopBarViewModel("DungeonApp");
        Sidebar = new GlobalSidebarViewModel(OnSectionSelected);
        StatusBar = new StatusBarViewModel("Gotowe");

        // Built once and reused, not recreated per navigation: it owns the open panels and the desk
        // arrangement, so rebuilding it on every visit would silently discard the user's layout.
        _campaignWorkspace = new CampaignWorkspaceViewModel(layoutStore);
        _currentWorkspaceContent = _campaignWorkspace;
    }

    public TopBarViewModel TopBar { get; }

    public GlobalSidebarViewModel Sidebar { get; }

    public StatusBarViewModel StatusBar { get; }

    public object CurrentWorkspaceContent
    {
        get => _currentWorkspaceContent;
        private set => SetField(ref _currentWorkspaceContent, value);
    }

    /// <summary>Writes anything the shell has pending. Called from the application's shutdown hooks.</summary>
    public void FlushPendingState() => _campaignWorkspace.FlushLayout();

    // Temporary scaffolding until the real context router exists: it dispatches on the section id
    // rather than its label, and every other section still lands on the placeholder.
    private void OnSectionSelected(NavigationItemViewModel section) =>
        CurrentWorkspaceContent = section.Id == CampaignsSectionId
            ? _campaignWorkspace
            : new WorkspacePlaceholderViewModel(section.Label);
}
