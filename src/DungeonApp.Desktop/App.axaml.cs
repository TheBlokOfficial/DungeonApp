using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DungeonApp.Application.Campaigns;
using DungeonApp.Desktop.ViewModels;
using DungeonApp.Infrastructure.Campaigns.Persistence;

namespace DungeonApp.Desktop;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
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
            var viewModel = new MainWindowViewModel(
                new CreateCampaignUseCase(repository, moduleCatalog),
                new AdvanceCampaignTimeUseCase(repository),
                new GetCampaignSessionUseCase(repository),
                new ListCampaignsUseCase(repository),
                new EnableCampaignModuleUseCase(repository),
                new ScheduleWorldEventUseCase(repository));

            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            viewModel.LoadCampaignsCommand.Execute(null);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
