using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using DungeonApp.Desktop.Themes;

namespace MockupRenderer;

/// <summary>
/// Headlessowy renderer pojedynczego pliku .axaml do PNG — narzędzie deweloperskie
/// do sprawdzania mockupów z <c>design/mockups/</c> bez uruchamiania całej aplikacji.
/// Zobacz README.md w tym katalogu po składnię wywołania.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (!CliOptions.TryParse(args, out var options, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(CliOptions.Usage);
            return 1;
        }

        if (!File.Exists(options.InputPath))
        {
            Console.Error.WriteLine($"Plik wejściowy nie istnieje: {options.InputPath}");
            return 1;
        }

        try
        {
            Render(options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Renderowanie nie powiodło się: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"Zapisano {options.OutputPath}");
        return 0;
    }

    private static void Render(CliOptions options)
    {
        var builder = AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
        builder.SetupWithoutStarting();

        var app = (App)Application.Current!;
        UiScaleProfiles.Apply(app, options.ScaleProfile);

        var xaml = File.ReadAllText(options.InputPath);
        var control = (Control)AvaloniaRuntimeXamlLoader.Load(xaml, typeof(DungeonApp.Desktop.App).Assembly);

        if (options.DataContextPath is not null)
        {
            control.DataContext = JsonDataContextFactory.Create(options.DataContextPath);
        }

        var window = new Window
        {
            Width = options.Width,
            Height = options.Height,
            Content = control,
        };
        window.Show();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        using var bitmap = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException("Nie udało się przechwycić klatki renderu.");

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(options.OutputPath));
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        bitmap.Save(options.OutputPath);
        window.Close();
    }
}
