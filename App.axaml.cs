using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using DungeonApp.ViewModels;
using DungeonApp.ViewModels.Dashboard;
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
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IContentRegistry, ContentRegistry>();
        services.AddSingleton<ITranslationService, TranslationService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        
        // Dashboard Tabs
        services.AddSingleton<CampaignsTabViewModel>();
        services.AddSingleton<CharactersTabViewModel>();
        services.AddSingleton<ItemsTabViewModel>();
        services.AddSingleton<AdversariesTabViewModel>();
        services.AddSingleton<OthersTabViewModel>();
        services.AddSingleton<SettingsTabViewModel>();
        services.AddSingleton<SandboxTabViewModel>();
        
        services.AddTransient<CreateCampaignViewModel>();
        services.AddTransient<CreateCharacterViewModel>();

        // Campaign Sub-Tabs — tworzone dynamicznie przez ActivatorUtilities.CreateInstance
        // w CampaignDetailViewModel, nie przez kontener DI (rejestracja celowo pominięta).

        Services = services.BuildServiceProvider();

        // Wymuszenie natychmiastowej inicjalizacji ContentRegistry przy starcie aplikacji.
        // Bez tego Singleton jest "lazy" i nie wygeneruje domyślnej paczki na dysk,
        // dopóki użytkownik nie wejdzie w zakładkę Rejestru.
        Services.GetRequiredService<IContentRegistry>();

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
