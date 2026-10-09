using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Content.Dnd5e;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Entries.ContentTab;
using DungeonApp.Desktop.Entries.Controls;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.Workspace;
using DungeonApp.Desktop.Shell.Gallery;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Workspace.Layout;
using SkiaSharp;

namespace DungeonApp.Tools.Render;

/// <summary>
/// Boots the real shell (real App, real main window, the real system reading the bundled pack) on
/// the headless platform with Skia rasterization, so a look can be checked without a screen. Writes
/// one PNG per content card and per gallery section into the directory given as the first argument.
/// Nothing reads the GM's Documents: packs come from the build output, desk layouts go to a
/// throwaway directory.
/// </summary>
internal static class Program
{
    private const int WindowWidth = 1440;
    private const int WindowHeight = 900;

    private static string _outDir = "";

    /// <summary>
    /// Pack directories read after the bundled one - the test fixtures, so the cards they exercise
    /// are rendered too.
    /// </summary>
    private static string[] _extraPacks = [];

    private static readonly HashSet<string> _written = new(StringComparer.Ordinal);

    /// <summary>
    /// The file-name pattern from <c>--only=</c>, or null to render everything. Selecting a row and
    /// letting it settle is what a run spends its time on, so a row whose file would not match is
    /// never selected.
    /// </summary>
    private static Regex? _only;

    private static string _onlyHead = "";

    /// <summary>
    /// Arguments: the output directory, then any number of extra pack directories, and optionally
    /// <c>--only=&lt;pattern&gt;</c> - a wildcard over file names without the extension.
    /// </summary>
    [STAThread]
    private static int Main(string[] args)
    {
        var only = args.FirstOrDefault(arg => arg.StartsWith("--only=", StringComparison.Ordinal))?["--only=".Length..];
        if (!string.IsNullOrEmpty(only))
        {
            _only = new Regex("^" + Regex.Escape(only).Replace(@"\*", ".*", StringComparison.Ordinal).Replace(@"\?", ".", StringComparison.Ordinal) + "$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _onlyHead = only[..(only.IndexOfAny(['*', '?']) is var wildcard and >= 0 ? wildcard : only.Length)];
        }

        args = [.. args.Where(arg => !arg.StartsWith("--only=", StringComparison.Ordinal))];
        _outDir = Path.GetFullPath(args.Length > 0 ? args[0] : "out");
        _extraPacks = [.. args.Skip(1).Select(Path.GetFullPath), WriteToolPack()];
        Directory.CreateDirectory(_outDir);

        var lifetime = new ClassicDesktopStyleApplicationLifetime { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        // The campaigns the shell reads and writes live here, never in the GM's Documents.
        var documents = Path.Combine(Path.GetTempPath(), "DungeonAppRender", "documents");
        if (Directory.Exists(documents))
        {
            Directory.Delete(documents, recursive: true);
        }

        Directory.CreateDirectory(documents);

        var layoutStore = new WorkspaceLayoutStore(Path.Combine(Path.GetTempPath(), "DungeonAppRender"));
        AppBuilder.Configure(() => new DungeonApp.Desktop.App(BuildSystems()) { DocumentsPath = documents, LayoutStore = layoutStore })
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont()
            .SetupWithLifetime(lifetime);

        var window = lifetime.MainWindow ?? throw new InvalidOperationException("The app did not create its main window.");
        var shell = (AppShellViewModel)window.DataContext!;
        window.Width = WindowWidth;
        window.Height = WindowHeight;
        window.Show();

        Pump(() => shell.IsReady, "startup");
        Settle();

        shell.SystemSelection.Systems.Single().ChooseCommand.Execute(null);
        Pump(() => shell.IsSystemChosen && shell.Sidebar is not null, "system choice");
        Settle();

        RenderCards(window, shell, "Stworzenia", "creature");
        RenderCards(window, shell, "Przedmioty", "gear");
        RenderCards(window, shell, "Stany", "condition");
        if (MayMatch("gallery_"))
        {
            RenderGallery(window, shell);
        }

        if (MayMatch("desk_") || MayMatch("sidebar_") || MayMatch("window_"))
        {
            RenderDesk(window, shell);
        }

        Console.WriteLine(_written.Count == 0 ? "Nothing matched the pattern." : $"Done: {_written.Count} file(s).");
        return 0;
    }

    private static bool Wants(string stem) => _only?.IsMatch(stem) ?? true;

    /// <summary>
    /// Whether any file starting with <paramref name="prefix"/> could match the pattern, judged by its
    /// literal head: a whole tab or the gallery is skipped without being opened.
    /// </summary>
    private static bool MayMatch(string prefix) =>
        _only is null
        || _onlyHead.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
        || prefix.StartsWith(_onlyHead, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A packs directory with one pack the tool builds itself, in a throwaway location, for the cards neither the bundled pack
    /// nor the fixtures have: a commoner's statblock, to see the card of a creature with the plainest
    /// values, and a condition with no icon whose rules come in named parts, to see the empty icon
    /// place in its row and a list of levels on its card. Written fresh on every run so no earlier
    /// file lingers.
    /// </summary>
    private static string WriteToolPack()
    {
        var root = Path.Combine(Path.GetTempPath(), "DungeonAppRender", "tool-packs");
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }

        var pack = Path.Combine(root, "render-tool");
        Directory.CreateDirectory(Path.Combine(pack, "entries"));
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        File.WriteAllText(Path.Combine(pack, "pack.json"), """
            {
              "formatVersion": 1,
              "id": "render-tool",
              "name": "Karty narzędzia",
              "version": { "major": 1, "minor": 0 }
            }
            """, utf8);
        File.WriteAllText(Path.Combine(pack, "entries", "karczmarz.json"), """
            {
              "id": "karczmarz",
              "name": "Karczmarz",
              "template": "dnd5e:creature",
              "templateVersion": 1,
              "values": {
                "size": "Średni",
                "type": "humanoid",
                "alignment": "praworządny dobry",
                "combat": { "ac": 10, "hp": 4, "hpDice": "1k8", "str": 10, "dex": 10, "con": 10, "int": 10, "wis": 10, "cha": 10 },
                "speed": "9 m",
                "senses": "pasywna Percepcja 11",
                "languages": "wspólny",
                "challenge": "0",
                "xp": 10,
                "actions": {
                  "entries": [
                    {
                      "name": "Pałka",
                      "text": "Atak bronią w zwarciu: **+2 do trafienia**, zasięg 1,5 m, jeden cel. Trafienie: **2 (1k4)** obrażeń obuchowych."
                    }
                  ]
                },
                "description": "Zna każdą plotkę w okolicy i każdego, kto jej nie zna, wita jak starego znajomego."
              }
            }
            """, utf8);
        File.WriteAllText(Path.Combine(pack, "entries", "wyczerpany.json"), """
            {
              "id": "wyczerpany",
              "name": "Wyczerpany",
              "template": "dnd5e:condition",
              "templateVersion": 1,
              "values": {
                "summary": "Sześć poziomów, każdy dokłada swoje; szósty to śmierć.",
                "rules": {
                  "intro": "Wyczerpanie mierzy się w sześciu poziomach. Skutki się kumulują.",
                  "entries": [
                    { "name": "Poziom 1", "text": "**Utrudnienie** w testach cech." },
                    { "name": "Poziom 2", "text": "Szybkość zmniejszona o połowę." },
                    { "name": "Poziom 6", "text": "Śmierć." }
                  ]
                }
              }
            }
            """, utf8);
        return root;
    }

    private static IReadOnlyList<IGameSystem> BuildSystems()
    {
        var system = SystemId.Create(Dnd5eSystem.IdValue);
        return [new Dnd5eSystem([SystemDirectories.BundledPacks(AppContext.BaseDirectory, system), .. _extraPacks])];
    }

    /// <summary>
    /// Selects every row of a content tab and saves its detail block - header and card - then the
    /// whole tab, list included, with its first row selected.
    /// </summary>
    private static void RenderCards(Window window, AppShellViewModel shell, string tabTitle, string prefix)
    {
        if (!MayMatch($"{prefix}_") && !MayMatch($"tab_{prefix}"))
        {
            return;
        }

        shell.Sidebar!.SystemTabItems.Single(item => item.Label == tabTitle).SelectCommand.Execute(null);
        Pump(() => shell.CurrentWorkspaceContent is Control { DataContext: ContentTabViewModel tab } && tab.Title == tabTitle, tabTitle);
        Settle();

        var tabView = (Control)shell.CurrentWorkspaceContent!;
        var tabModel = (ContentTabViewModel)tabView.DataContext!;
        foreach (var row in tabModel.Sections.SelectMany(section => section.Rows).ToList())
        {
            if (!Wants($"{prefix}_{Slug(row.Name)}"))
            {
                continue;
            }

            row.SelectCommand.Execute(null);
            SetSize(window, WindowWidth, WindowHeight);
            GrowToFit(window, tabView);

            var detail = tabView.GetVisualDescendants().OfType<EntryDetailView>().FirstOrDefault(view => view.IsEffectivelyVisible);
            if (detail is not null)
            {
                SaveCrop(window, (Control)detail.GetVisualParent()!, UniqueName($"{prefix}_{Slug(row.Name)}"));
            }
        }

        SetSize(window, WindowWidth, WindowHeight);
        if (Wants($"tab_{prefix}"))
        {
            tabModel.Sections.SelectMany(section => section.Rows).FirstOrDefault()?.SelectCommand.Execute(null);
            SaveCrop(window, tabView, UniqueName($"tab_{prefix}"));
        }
    }

    /// <summary>
    /// A file name not yet written in this run: two packs may each have an entry of the same name
    /// (the bundled pack and a fixture), and the second must not overwrite the first.
    /// </summary>
    private static string UniqueName(string stem)
    {
        var name = $"{stem}.png";
        for (var copy = 2; !_written.Add(name); copy++)
        {
            name = $"{stem}-{copy}.png";
        }

        return name;
    }
    /// <summary>Opens the controls gallery tall enough to show it whole and saves each section on its own.</summary>
    private static void RenderGallery(Window window, AppShellViewModel shell)
    {
        shell.Sidebar!.GalleryItem.SelectCommand.Execute(null);
        Pump(() => window.GetVisualDescendants().OfType<GalleryView>().Any(), "gallery");
        Settle();

        var gallery = window.GetVisualDescendants().OfType<GalleryView>().First();
        GrowToFit(window, gallery);

        // The sections' own column: the one panel whose children are the section views.
        var list = gallery.GetVisualDescendants().OfType<StackPanel>()
            .First(panel => panel.Children.Count > 0 && panel.Children.All(child => child.GetType().Name.EndsWith("Section", StringComparison.Ordinal)));
        foreach (var section in list.Children)
        {
            var stem = $"gallery_{Slug(section.GetType().Name.Replace("Section", "", StringComparison.Ordinal))}";
            if (Wants(stem))
            {
                SaveCrop(window, section, UniqueName(stem));
            }
        }

        SetSize(window, WindowWidth, WindowHeight);
    }

    /// <summary>
    /// Creates a campaign on the shelf, opens it through its row, brings two goblins into the world
    /// tool, and saves the sidebar on the shelf and with the campaign open, and the desk empty, with
    /// the world window, and with it maximized.
    /// </summary>
    private static void RenderDesk(Window window, AppShellViewModel shell)
    {
        SetSize(window, WindowWidth, WindowHeight);
        shell.Sidebar!.CampaignPositionItem.SelectCommand.Execute(null);
        Settle();

        if (Wants("sidebar_shelf"))
        {
            SaveCrop(window, window.GetVisualDescendants().OfType<GlobalSidebarView>().First(), UniqueName("sidebar_shelf"));
        }

        var library = (CampaignLibraryViewModel)shell.CurrentWorkspaceContent!;
        library.NewCampaignName = "Kopalnia Phandelver";
        Await(library.CreateCommand.ExecuteAsync(null), "campaign creation");
        Pump(() => library.Campaigns.Count > 0, "shelf");
        Await(library.Campaigns.Single().OpenCommand.ExecuteAsync(null), "campaign opening");
        Pump(() => shell.CurrentWorkspaceContent is Control { DataContext: CampaignWorkspaceViewModel }, "desk");
        Settle();

        if (Wants("sidebar_campaign"))
        {
            SaveCrop(window, window.GetVisualDescendants().OfType<GlobalSidebarView>().First(), UniqueName("sidebar_campaign"));
        }

        var deskView = (Control)shell.CurrentWorkspaceContent!;
        var desk = (CampaignWorkspaceViewModel)deskView.DataContext!;
        var panel = desk.Panels.Single(candidate => candidate.Title == "Świat kampanii");

        // Added through the tool's own model, as the GM would pick them in its list.
        var tool = (CampaignInstancesToolViewModel)((Control)panel.Body).DataContext!;
        for (var count = 0; count < 3; count++)
        {
            tool.SelectedToAdd = tool.AddableEntries.First(option => option.Name.Contains("Goblin", StringComparison.OrdinalIgnoreCase));
            Await(tool.AddCommand.ExecuteAsync(null), "adding a goblin");
        }

        Settle();

        if (Wants("desk_empty"))
        {
            panel.MinimizeCommand.Execute(null);
            SaveCrop(window, deskView, UniqueName("desk_empty"));
        }

        panel.ActivateCommand.Execute(null);
        Settle();
        if (Wants("desk_window"))
        {
            SaveCrop(window, deskView, UniqueName("desk_window"));
        }

        panel.ToggleMaximizeCommand.Execute(null);
        Settle();
        if (Wants("desk_maximized"))
        {
            SaveCrop(window, deskView, UniqueName("desk_maximized"));
        }

        if (Wants("window_full"))
        {
            SaveCrop(window, window, UniqueName("window_full"));
        }
    }

    /// <summary>Runs the dispatcher until <paramref name="task"/> finishes, then surfaces its failure if any.</summary>
    private static void Await(Task task, string what)
    {
        Pump(() => task.IsCompleted, what);
        task.GetAwaiter().GetResult();
    }

    /// <summary>Enlarges the window until no visible scroll viewer under <paramref name="root"/> scrolls vertically.</summary>
    private static void GrowToFit(Window window, Control root)
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            var overflow = root.GetVisualDescendants().OfType<ScrollViewer>()
                .Where(viewer => viewer.IsEffectivelyVisible)
                .Select(viewer => viewer.Extent.Height - viewer.Viewport.Height)
                .DefaultIfEmpty(0)
                .Max();
            if (overflow <= 0.5)
            {
                return;
            }

            SetSize(window, window.Width, Math.Ceiling(window.Height + overflow + 2));
        }
    }

    private static void SetSize(Window window, double width, double height)
    {
        window.Width = width;
        window.Height = height;
        Settle();
    }

    private static void SaveCrop(Window window, Control control, string name)
    {
        Settle();
        var origin = control.TranslatePoint(new Point(0, 0), window)
            ?? throw new InvalidOperationException($"{name}: the control is not in the window.");

        var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
        using var stream = new MemoryStream();
        frame.Save(stream);
        stream.Position = 0;
        using var bitmap = SKBitmap.Decode(stream);

        var left = Math.Max(0, (int)origin.X);
        var top = Math.Max(0, (int)origin.Y);
        var right = Math.Min(bitmap.Width, (int)Math.Ceiling(origin.X + control.Bounds.Width));
        var bottom = Math.Min(bitmap.Height, (int)Math.Ceiling(origin.Y + control.Bounds.Height));
        using var subset = new SKBitmap();
        bitmap.ExtractSubset(subset, new SKRectI(left, top, right, bottom));
        using var image = SKImage.FromBitmap(subset);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        var target = Path.Combine(_outDir, name);
        using (var file = File.Create(target))
        {
            data.SaveTo(file);
        }

        Console.WriteLine($"Saved {name} ({right - left}x{bottom - top})");
    }

    /// <summary>Runs the dispatcher and render ticks for a while, so asynchronous work and transitions finish.</summary>
    private static void Settle(int milliseconds = 700)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < milliseconds)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(10);
        }

        Dispatcher.UIThread.RunJobs();
    }

    private static void Pump(Func<bool> until, string what, int timeoutMilliseconds = 60000)
    {
        var watch = Stopwatch.StartNew();
        while (!until())
        {
            if (watch.ElapsedMilliseconds > timeoutMilliseconds)
            {
                throw new TimeoutException($"Timed out waiting for {what}.");
            }

            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(10);
        }
    }

    /// <summary>A file-name-safe form of a name: lower case, Polish letters without diacritics, dashes elsewhere.</summary>
    private static string Slug(string name)
    {
        var normalized = name.Replace('ł', 'l').Replace('Ł', 'L').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
        }

        return builder.ToString().Trim('-');
    }
}
