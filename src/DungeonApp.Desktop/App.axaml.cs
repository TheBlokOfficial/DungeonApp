using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DungeonApp.Application.Campaigns;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Themes;
using DungeonApp.Infrastructure.Campaigns.Persistence;

namespace DungeonApp.Desktop;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        UiScaleProfiles.Apply(this, Program.RequestedUiScaleProfile);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var campaignDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DungeonApp",
                "campaigns");
            var moduleCatalog = CampaignModuleCatalogFactory.CreateDefault();
            var repository = new JsonCampaignRepository(campaignDirectory, moduleFactories: moduleCatalog.Factories);
            var campaignLibrary = new CampaignLibraryViewModel(
                new CreateCampaignUseCase(repository, moduleCatalog),
                new GetCampaignSessionUseCase(repository),
                new ListCampaignsUseCase(repository));
            var viewModel = new AppShellViewModel(campaignLibrary);

            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            campaignLibrary.LoadCampaignsCommand.Execute(null);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
