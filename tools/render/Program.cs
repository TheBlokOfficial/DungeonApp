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
using DungeonApp.Core.Entries;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Workspace.Controls;
using DungeonApp.Desktop.Workspace.Panels;
using DungeonApp.Desktop.Workspace.World;
using DungeonApp.Desktop.Entries.ContentTab;
using DungeonApp.Desktop.Entries.Controls;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.Workspace;
using DungeonApp.Desktop.Shell.Gallery;
using DungeonApp.Desktop.Shell.Gallery.Sections;
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
internal static partial class Program
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
        if (MayMatch("icons_"))
        {
            RenderIconSheets();
        }

        if (MayMatch("gallery_"))
        {
            RenderGallery(window, shell);
        }

        if (MayMatch("desk_") || MayMatch("entity_card_") || MayMatch("sidebar_") || MayMatch("window_") || MayMatch("szklo"))
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
            if (section is LivePartSection livePart)
            {
                RenderLivePart(window, livePart, stem);
            }
            else if (Wants(stem))
            {
                SaveCrop(window, section, UniqueName(stem));
            }

            if (section is MenusSection menus)
            {
                RenderOpenMenu(window, menus, stem);
            }
        }

        SetSize(window, WindowWidth, WindowHeight);
    }

    /// <summary>
    /// The icon button's menu opened with two submenu levels, saved as the whole window (the menus
    /// are popups, outside the section's bounds): shortcuts and submenu arrows against the right edge
    /// and the gap between a menu and its submenu.
    /// </summary>
    private static void RenderOpenMenu(Window window, MenusSection section, string stem)
    {
        var name = $"{stem}_open";
        if (!Wants(name))
        {
            return;
        }

        ClickOn(window, section.GetVisualDescendants().OfType<Button>().First());
        Control Item(string header) => window.GetVisualDescendants().OfType<MenuItem>()
            .First(item => item.IsEffectivelyVisible && item.Header as string == header);
        ClickOn(window, Item("Widok"));
        ClickOn(window, Item("Sortuj według"));
        // Crop to the menus and their submenus: the box around every visible item of the open levels.
        var items = window.GetVisualDescendants().OfType<MenuItem>().Where(item => item.IsEffectivelyVisible).ToList();
        var boxes = items.Select(item => new Rect(item.TranslatePoint(new Point(0, 0), window)!.Value, item.Bounds.Size)).ToList();
        var left = boxes.Min(box => box.Left) - 24;
        var top = boxes.Min(box => box.Top) - 24;
        var crop = new Rect(left, top, boxes.Max(box => box.Right) + 24 - left, boxes.Max(box => box.Bottom) + 24 - top);
        SaveCrop(window, window, UniqueName(name), crop);
        PressKey(window, Avalonia.Input.Key.Escape);
        Rest(window);
    }

    /// <summary>
    /// The live part's controls at twice their size: the whole section at rest, then each state that
    /// only the pointer or the keyboard reaches (hover, editing, a typed change), posed with real input
    /// on the control and saved as a crop of that control's sample. Every pose is undone before the
    /// next one, so the poses do not leak into each other.
    /// </summary>
    private static void RenderLivePart(Window window, LivePartSection section, string stem)
    {
        if (Wants(stem))
        {
            SaveScaled(section, UniqueName(stem));
        }

        // One block per control: find its sample in the section by name, pose it with HoverOver,
        // ClickOn, TypeText and PressKey, save it with SaveScaled under "{stem}_<control>_<state>",
        // then Rest(window) to undo the pose.
        RenderLiveValue(window, section, $"{stem}_value");

        var rows = section.FindControl<StackPanel>("MarkerRows")!;
        if (Wants($"{stem}_rows"))
        {
            Rest(window);
            SaveScaled(rows, UniqueName($"{stem}_rows"));
        }

        if (Wants($"{stem}_rows_hover"))
        {
            HoverOver(window, section.FindControl<Control>("ConditionRow")!.GetVisualDescendants().OfType<Button>().First());
            SaveScaled(rows, UniqueName($"{stem}_rows_hover"));
            Rest(window);
        }

        // Note: the crop is the whole group of samples, which leaves room for the frame the note
        // draws outside its bounds.
        var notes = section.FindControl<Border>("NoteSamples")!;
        var noteLines = section.FindControl<DungeonApp.Desktop.Controls.EditableText>("NoteLines")!;
        var noteEmpty = section.FindControl<DungeonApp.Desktop.Controls.EditableText>("NoteEmpty")!;
        var noteShort = section.FindControl<DungeonApp.Desktop.Controls.EditableText>("NoteShort")!;
        var original = noteLines.Text;
        if (Wants($"{stem}_note_hover"))
        {
            HoverOver(window, noteLines);
            SaveScaled(notes, UniqueName($"{stem}_note_hover"));
            Rest(window);
        }

        if (Wants($"{stem}_note_edit"))
        {
            ClickOn(window, noteLines);
            SaveScaled(notes, UniqueName($"{stem}_note_edit"));
            Rest(window);
        }

        if (Wants($"{stem}_note_typed"))
        {
            ClickOn(window, noteLines);
            noteLines.CaretIndex = noteLines.Text?.Length ?? 0;
            PressKey(window, Avalonia.Input.Key.Enter);
            TypeText(window, "Zna hasło do bramy.");
            SaveScaled(notes, UniqueName($"{stem}_note_typed"));
            noteLines.Text = original;
            Rest(window);
        }

        if (Wants($"{stem}_note_empty_hover"))
        {
            HoverOver(window, noteEmpty);
            SaveScaled(notes, UniqueName($"{stem}_note_empty_hover"));
            Rest(window);
        }

        if (Wants($"{stem}_note_short_hover"))
        {
            HoverOver(window, noteShort);
            SaveScaled(notes, UniqueName($"{stem}_note_short_hover"));
            Rest(window);
        }
    }

    /// <summary>
    /// The working tracked value in its pointer and keyboard states, saved as its row (the value and
    /// its neighbours), so a shift between text and field shows against the neighbours.
    /// </summary>
    private static void RenderLiveValue(Window window, LivePartSection section, string stem)
    {
        var tile = section.FindControl<DungeonApp.Desktop.Controls.TrackedValueTile>("LiveValue")
                   ?? throw new InvalidOperationException("The live part has no LiveValue sample.");
        var row = section.FindControl<Control>("LiveValueRow")
                  ?? throw new InvalidOperationException("The live part has no LiveValueRow.");
        var number = tile.GetVisualDescendants().OfType<DungeonApp.Desktop.Controls.EditableText>().First();

        void Save(string state)
        {
            if (Wants($"{stem}_{state}"))
            {
                SaveScaled(row, UniqueName($"{stem}_{state}"));
            }
        }

        HoverOver(window, number);
        Save("hover");
        Rest(window);

        ClickOn(window, number);
        Save("edit");
        TypeText(window, "-5");
        Save("typed");
        Rest(window);

        ClickOn(window, number);
        TypeText(window, "12");
        Save("set");
        Rest(window);

        ClickOn(window, number);
        TypeText(window, "-120");
        Save("long");
        Rest(window);

        number.Focus(Avalonia.Input.NavigationMethod.Tab);
        Settle();
        Save("focus");
        Rest(window);
    }

    /// <summary>Moves the pointer to the middle of <paramref name="control"/>.</summary>
    private static void HoverOver(Window window, Control control)
    {
        window.MouseMove(Middle(window, control));
        Settle();
    }

    /// <summary>Presses and releases the left button in the middle of <paramref name="control"/>.</summary>
    private static void ClickOn(Window window, Control control)
    {
        var point = Middle(window, control);
        window.MouseMove(point);
        window.MouseDown(point, Avalonia.Input.MouseButton.Left);
        window.MouseUp(point, Avalonia.Input.MouseButton.Left);
        Settle();
    }

    private static void TypeText(Window window, string text)
    {
        window.KeyTextInput(text);
        Settle();
    }

    private static void PressKey(Window window, Avalonia.Input.Key key)
    {
        window.KeyPress(key, Avalonia.Input.RawInputModifiers.None, Avalonia.Input.PhysicalKey.None, null);
        window.KeyRelease(key, Avalonia.Input.RawInputModifiers.None, Avalonia.Input.PhysicalKey.None, null);
        Settle();
    }

    /// <summary>Takes the pointer off every control and the focus off every field.</summary>
    private static void Rest(Window window)
    {
        window.MouseMove(new Point(-1, -1));
        window.FocusManager?.Focus(null);
        Settle();
    }

    private static Point Middle(Window window, Control control) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)
        ?? throw new InvalidOperationException($"{control.Name ?? control.GetType().Name} is not in the window.");

    /// <summary>
    /// Creates a campaign on the shelf, opens it through its row, fills the world catalog from the
    /// bundled pack, and saves the sidebar on the shelf and with the campaign open, and the desk empty,
    /// with the catalog expanded, with a window over it and collapsed.
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
        Await(library.Campaigns.First().OpenCommand.ExecuteAsync(null), "campaign opening");
        Pump(() => shell.CurrentWorkspaceContent is Control { DataContext: CampaignWorkspaceViewModel }, "desk");
        Settle();

        if (Wants("sidebar_campaign"))
        {
            SaveCrop(window, window.GetVisualDescendants().OfType<GlobalSidebarView>().First(), UniqueName("sidebar_campaign"));
        }

        var deskView = (Control)shell.CurrentWorkspaceContent!;
        var desk = (CampaignWorkspaceViewModel)deskView.DataContext!;
        var catalog = desk.Catalog;

        if (Wants("desk_empty"))
        {
            SaveCrop(window, deskView, UniqueName("desk_empty"));
        }

        // Folders Karczma and Kopalnia > Jaskinia goblinów > Skarbiec, entities from the bundled pack.
        EntryAddress Srd(string entry) => new(ContentId.Create("dnd5e-srd"), ContentId.Create(entry));
        Await(catalog.CreateFolderAsync(null, "Karczma"), "tavern folder");
        var tavern = catalog.SelectedFolders.Single();
        Await(catalog.CreateFolderAsync(null, "Kopalnia"), "mine folder");
        var mine = catalog.SelectedFolders.Single();
        Await(catalog.CreateFolderAsync(mine, "Jaskinia goblinów"), "cave folder");
        var cave = catalog.SelectedFolders.Single();
        Await(catalog.CreateFolderAsync(cave, "Skarbiec"), "vault folder");
        var vault = catalog.SelectedFolders.Single();
        Await(catalog.AddEntitiesAsync(Srd("szkielet"), 1, cave, "Goblin 10"), "goblin 10");
        Await(catalog.AddEntitiesAsync(Srd("szkielet"), 1, cave, "Goblin 2"), "goblin 2");
        Await(catalog.AddEntitiesAsync(Srd("wilk"), 1, cave), "wolf");
        Await(catalog.AddEntitiesAsync(Srd("lina-konopna"), 1, vault), "rope");
        Await(catalog.AddEntitiesAsync(Srd("mikstura-leczenia"), 1, vault), "potion");
        Await(catalog.AddEntitiesAsync(Srd("rycerz"), 1, tavern, "Kapitan Varga"), "knight");
        Await(catalog.AddEntitiesAsync(Srd("nie-ma-takiego-wpisu"), 1, tavern), "missing entry");
        Settle();

        WorldRowViewModel Row(string name) => catalog.Rows.First(row => row.Name == name);
        catalog.Click(Row("Goblin 2"), ctrl: false, shift: false);
        catalog.Click(Row("Wilk"), ctrl: true, shift: false);
        catalog.Click(Row("Kapitan Varga"), ctrl: true, shift: false);
        Settle();

        if (Wants("desk_catalog"))
        {
            SaveCrop(window, deskView, UniqueName("desk_catalog"));
        }

        if (Wants("desk_catalog_rename"))
        {
            var renamed = Row("Jaskinia goblinów");
            catalog.BeginRename(renamed);
            Settle();
            SaveCrop(window, deskView, UniqueName("desk_catalog_rename"));
            catalog.CancelRename(renamed);
            Settle();
        }

        if (Wants("desk_catalog_window"))
        {
            var descriptor = new WorkspacePanelDescriptor(
                "render-window",
                "Okno",
                "DungeonIconSkull",
                WorkspacePanelGroup.Session,
                new PanelPlacement(catalog.X - 260, catalog.Y + 80, 420, 300),
                new PanelConstraints(200, 120, 2000, 2000),
                () => new TextBlock { Text = "Okno nad katalogiem" });
            var panel = new WorkspacePanelViewModel(desk, descriptor, descriptor.Id, descriptor.DefaultPlacement);
            desk.Panels.Add(panel);
            panel.ActivateCommand.Execute(null);
            Settle();
            SaveCrop(window, deskView, UniqueName("desk_catalog_window"));
            desk.Panels.Remove(panel);
            Settle();
        }

        if (Wants("desk_palette"))
        {
            // A window half under the palette, so the glass has both a window and the bare desk behind it.
            var descriptor = new WorkspacePanelDescriptor(
                "render-palette-window",
                "Okno pod paletą",
                "DungeonIconSkull",
                WorkspacePanelGroup.Session,
                new PanelPlacement(WindowWidth / 2 - 420, 20, 460, 320),
                new PanelConstraints(200, 120, 2000, 2000),
                () => new TextBlock { Text = "Okno pod paletą: tekst, który ma się rozmyć", TextWrapping = Avalonia.Media.TextWrapping.Wrap });
            var panel = new WorkspacePanelViewModel(desk, descriptor, descriptor.Id, descriptor.DefaultPlacement);
            desk.Panels.Add(panel);
            panel.ActivateCommand.Execute(null);
            Settle();
            var catalogView = deskView.GetVisualDescendants().OfType<WorldCatalogView>().First();
            catalogView.ShowAddPalette(catalog.ResolveAddTarget());
            Settle();
            SaveCrop(window, window, UniqueName("desk_palette"));
            PressKey(window, Avalonia.Input.Key.Escape);
            desk.Panels.Remove(panel);
            Settle();
        }

        // Full-size window with the palette over a desk that has windows under it, once per glass tint opacity.
        // The tint is swapped in the window resources only for the length of each render.
        (string Stem, byte Alpha)[] glassVariants = [("szklo-55", 0x8C), ("szklo-70", 0xB3), ("szklo-80", 0xCC), ("szklo-90", 0xE6)];
        if (Wants("szklo") || glassVariants.Any(variant => Wants(variant.Stem)))
        {
            SetSize(window, 1920, 1080);
            // A creature preview and a pinned item card together cover the whole palette rectangle
            // (about 700-1220 x 25-465) and stick out on every side, so the glass is judged over real, dense content.
            var preview = desk.OpenPreview();
            catalog.Click(Row("Goblin 2"), ctrl: false, shift: false);
            var pinned = desk.PinEntity(Row("Mikstura leczenia").EntityId!.Value)!;
            catalog.Click(Row("Goblin 2"), ctrl: false, shift: false);
            // Desk coordinates start right of the sidebar (224 px), so these are 560 and 1030 on screen.
            preview.X = 336;
            preview.Y = 10;
            preview.Width = 480;
            preview.Height = 520;
            pinned.X = 806;
            pinned.Y = 10;
            pinned.Width = 340;
            pinned.Height = 520;
            Settle();
            var catalogView = deskView.GetVisualDescendants().OfType<WorldCatalogView>().First();
            catalogView.ShowAddPalette(catalog.ResolveAddTarget());
            Settle();
            var hadTint = window.Resources.TryGetValue("DungeonPaletteGlassTintBrush", out var originalTint);
            foreach (var (stem, alpha) in glassVariants)
            {
                if (!Wants("szklo") && !Wants(stem))
                {
                    continue;
                }

                window.Resources["DungeonPaletteGlassTintBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0x1F, 0x1D, 0x1A));
                Settle();
                SaveCrop(window, window, UniqueName(stem));
            }

            if (hadTint)
            {
                window.Resources["DungeonPaletteGlassTintBrush"] = originalTint;
            }
            else
            {
                window.Resources.Remove("DungeonPaletteGlassTintBrush");
            }

            PressKey(window, Avalonia.Input.Key.Escape);
            desk.Close(pinned);
            desk.Close(preview);

            SetSize(window, WindowWidth, WindowHeight);
            Settle();
        }

        if (Wants("desk_catalog_collapsed"))
        {
            catalog.SetExpanded(catalog.Rows[0], false);
            Settle();
            SaveCrop(window, deskView, UniqueName("desk_catalog_collapsed"));
            catalog.SetExpanded(catalog.Rows[0], true);
            Settle();
        }

        if (Wants("desk_catalog_drag"))
        {
            // Mid-drag: the three selected rows faded in place, their copy with "+2" under the pointer,
            // the frame on Skarbiec. Esc then puts everything back.
            var catalogView = deskView.GetVisualDescendants().OfType<WorldCatalogView>().First();
            Point RowPoint(string name)
            {
                var border = catalogView.GetVisualDescendants().OfType<Border>()
                    .First(candidate => candidate.Classes.Contains("world-row") && candidate.DataContext == Row(name));
                return border.TranslatePoint(new Point(70, border.Bounds.Height / 2), window)!.Value;
            }

            var from = RowPoint("Goblin 2");
            var to = RowPoint("Skarbiec");
            window.MouseMove(from);
            window.MouseDown(from, Avalonia.Input.MouseButton.Left);
            Settle();
            window.MouseMove(from + new Point(0, 6), Avalonia.Input.RawInputModifiers.LeftMouseButton);
            window.MouseMove(to, Avalonia.Input.RawInputModifiers.LeftMouseButton);
            Settle();
            SaveCrop(window, deskView, UniqueName("desk_catalog_drag"));
            PressKey(window, Avalonia.Input.Key.Escape);
            window.MouseUp(to, Avalonia.Input.MouseButton.Left);
            Settle();
        }

        if (Wants("desk_preview") || Wants("desk_preview_broken") || Wants("desk_preview_empty")
            || Wants("entity_card_creature") || Wants("entity_card_item") || Wants("entity_card_broken"))
        {
            // The window alone, at twice its size, to judge the header's details.
            void SaveWindow(WorkspacePanelViewModel panel, string name) =>
                SaveScaled(
                    deskView.GetVisualDescendants().OfType<PanelWindow>().First(candidate => ReferenceEquals(candidate.DataContext, panel)),
                    UniqueName(name));

            // Wide enough for the preview and a pinned card side by side, left of the catalog.
            SetSize(window, 1900, WindowHeight);
            Settle();
            var preview = desk.OpenPreview();
            catalog.Click(catalog.Rows.First(row => row.IsEntity && row.IsUnresolved), ctrl: false, shift: false);
            Settle();
            if (Wants("desk_preview_broken"))
            {
                SaveCrop(window, deskView, UniqueName("desk_preview_broken"));
            }

            if (Wants("entity_card_broken"))
            {
                SaveWindow(preview, "entity_card_broken");
            }

            var potion = Row("Mikstura leczenia");
            var pinned = desk.PinEntity(potion.EntityId!.Value)!;
            catalog.Click(Row("Goblin 2"), ctrl: false, shift: false);
            pinned.X = preview.X + preview.Width + 16;
            pinned.Y = preview.Y;
            Settle();
            if (Wants("desk_preview"))
            {
                SaveCrop(window, deskView, UniqueName("desk_preview"));
            }

            if (Wants("entity_card_creature"))
            {
                SaveWindow(preview, "entity_card_creature");
            }

            if (Wants("entity_card_item"))
            {
                SaveWindow(pinned, "entity_card_item");
            }

            desk.Close(pinned);

            // The preview is empty once the entity it showed is gone.
            // The temporary entity sits alone in the last folder, so its removal leaves the selection on a folder.
            Await(catalog.CreateFolderAsync(null, "Tymczasowy"), "temporary folder");
            var temporary = catalog.SelectedFolders.Single();
            Await(catalog.AddEntitiesAsync(Srd("wilk"), 1, temporary, "Do usunięcia"), "temporary entity");
            Await(catalog.DeleteSelectionAsync(), "temporary entity removal");
            Settle();
            if (Wants("desk_preview_empty"))
            {
                SaveCrop(window, deskView, UniqueName("desk_preview_empty"));
            }

            Await(catalog.DeleteSelectionAsync(), "temporary folder removal");

            desk.Close(preview);
            SetSize(window, WindowWidth, WindowHeight);
            Settle();
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

    /// <param name="window">The window whose frame is cut.</param>
    /// <param name="control">The control whose bounds are saved.</param>
    /// <param name="name">The file name.</param>
    /// <param name="area">Window-coordinate rectangle to save instead of the control's bounds.</param>
    private static void SaveCrop(Window window, Control control, string name, Rect? area = null)
    {
        Settle();
        var origin = area?.TopLeft ?? control.TranslatePoint(new Point(0, 0), window)
            ?? throw new InvalidOperationException($"{name}: the control is not in the window.");
        var size = area?.Size ?? control.Bounds.Size;

        var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
        using var stream = new MemoryStream();
        frame.Save(stream);
        stream.Position = 0;
        using var bitmap = SKBitmap.Decode(stream);

        var left = Math.Max(0, (int)origin.X);
        var top = Math.Max(0, (int)origin.Y);
        var right = Math.Min(bitmap.Width, (int)Math.Ceiling(origin.X + size.Width));
        var bottom = Math.Min(bitmap.Height, (int)Math.Ceiling(origin.Y + size.Height));
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

    /// <summary>
    /// Renders <paramref name="control"/> alone at twice its size - vector, not an enlarged bitmap - so
    /// a design's details can be judged. The control is drawn over the gallery's backstage colour,
    /// because on its own it has no background.
    /// </summary>
    private static void SaveScaled(Control control, string name)
    {
        Settle();
        var size = control.Bounds.Size;
        using var rendered = new Avalonia.Media.Imaging.RenderTargetBitmap(
            new PixelSize((int)Math.Ceiling(size.Width * 2), (int)Math.Ceiling(size.Height * 2)), new Vector(192, 192));
        rendered.Render(control);

        using var stream = new MemoryStream();
        rendered.Save(stream);
        stream.Position = 0;
        using var foreground = SKBitmap.Decode(stream);

        var backstage = control.TryFindResource("DungeonBackstageColor", out var resource) && resource is Avalonia.Media.Color color
            ? new SKColor(color.R, color.G, color.B, color.A)
            : SKColors.Black;
        using var surface = SKSurface.Create(new SKImageInfo(foreground.Width, foreground.Height));
        surface.Canvas.Clear(backstage);
        surface.Canvas.DrawBitmap(foreground, 0, 0);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using (var file = File.Create(Path.Combine(_outDir, name)))
        {
            data.SaveTo(file);
        }

        Console.WriteLine($"Saved {name} ({foreground.Width}x{foreground.Height})");
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
