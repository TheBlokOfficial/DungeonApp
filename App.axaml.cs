using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using DungeonApp.ViewModels;
using DungeonApp.Views;
using DungeonApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DungeonApp;

public partial class App : Application
{
    public new static App? Current => Application.Current as App;
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        
        // Services
        services.AddSingleton<IStorageService, FileStorageService>();
        services.AddSingleton<ICampaignService, CampaignService>();
        services.AddSingleton<ICharacterService, CharacterService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<CreateCampaignViewModel>();
        services.AddTransient<CreateCharacterViewModel>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowViewModel = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}