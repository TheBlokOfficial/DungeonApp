using System;
using System.ComponentModel;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Settings;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.Shell.StatusBar;
using DungeonApp.Desktop.Shell.TopBar;

namespace DungeonApp.Desktop.Shell;

public sealed class AppShellViewModel
{
    public AppShellViewModel(CampaignLibraryViewModel campaignLibrary, AppSettings settings, Action<AppSettings> applyAndPersistSettings)
    {
        CampaignLibrary = campaignLibrary;
        TopBar = new TopBarViewModel("Biblioteka Mistrza Gry");
        Sidebar = new GlobalSidebarViewModel(settings, applyAndPersistSettings);
        StatusBar = new StatusBarViewModel(campaignLibrary.StatusMessage);

        CampaignLibrary.PropertyChanged += OnCampaignLibraryPropertyChanged;
    }

    public TopBarViewModel TopBar { get; }

    public GlobalSidebarViewModel Sidebar { get; }

    public CampaignLibraryViewModel CampaignLibrary { get; }

    public StatusBarViewModel StatusBar { get; }

    private void OnCampaignLibraryPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(CampaignLibraryViewModel.StatusMessage))
        {
            StatusBar.Message = CampaignLibrary.StatusMessage;
        }
    }
}
