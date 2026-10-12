using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DungeonApp.Content.Dnd5e;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Workspace.Layout;

namespace DungeonApp.Tools.Render;

internal static partial class Program
{
    /// <summary>
    /// icons_sheet.png: every icon of the system's families side by side - the conditions of the
    /// bundled pack, the creature types and the item categories - each at the list row's size, a
    /// little larger and at the card frame's size, with the shell's Lucide skull at the head of the
    /// sheet as the measure of detail and weight.
    /// </summary>
    private static void RenderIconSheets()
    {
        if (!Wants("icons_sheet"))
        {
            return;
        }

        var skull = (DrawingImage)Avalonia.Application.Current!.FindResource("DungeonIconSkull")!;
        var conditions = Path.Combine(SystemDirectories.BundledPacks(AppContext.BaseDirectory, SystemId.Create(Dnd5eSystem.IdValue)),
            "dnd5e-srd", "images", "conditions");

        SaveSheet(
        [
            ("Wzorzec (Lucide)", [("skull", skull)]),
            ("Stany", [.. Directory.GetFiles(conditions, "*.svg").Order(StringComparer.Ordinal)
                .Select(file => (Path.GetFileNameWithoutExtension(file), (DrawingImage?)TryLoad(file)))]),
            ("Typy stworzeń", [.. KindIcons.CreatureTypes.Select(type => (type.Type, (DrawingImage?)KindIcons.Load(type.Icon))),
                ("(inny)", KindIcons.Load(KindIcons.DefaultCreatureIcon))]),
            ("Kategorie przedmiotów", [.. KindIcons.GearCategories.Select(category => (category.Category, (DrawingImage?)KindIcons.Load(category.Icon))),
                ("(inna)", KindIcons.Load(KindIcons.DefaultGearIcon))]),
        ], UniqueName("icons_sheet"));
    }

    private static DrawingImage? TryLoad(string file)
    {
        try
        {
            return SvgIcon.Load(file);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"{Path.GetFileName(file)}: {ex.Message}");
            return null;
        }
    }

    private static void SaveSheet(IReadOnlyList<(string Title, IReadOnlyList<(string Name, DrawingImage? Icon)> Icons)> families, string name)
    {
        var sheet = new StackPanel { Spacing = 16, Margin = new Avalonia.Thickness(16) };
        foreach (var (title, icons) in families)
        {
            sheet.Children.Add(new TextBlock { Text = title, Classes = { "body" } });
            var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (var (label, icon) in icons)
            {
                var sizes = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };
                foreach (var size in new[] { 16.0, 18, 24, 48 })
                {
                    var shown = new ForegroundIcon { Source = icon, Width = size, Height = size, VerticalAlignment = VerticalAlignment.Bottom };
                    // The row's size in the row's muted colour, the larger ones as the card frame draws them.
                    shown.Bind(ForegroundIcon.ForegroundProperty, shown.GetResourceObservable(size < 20 ? "DungeonTextMutedBrush" : "DungeonTextRegularBrush"));
                    sizes.Children.Add(shown);
                }

                wrap.Children.Add(new StackPanel
                {
                    Width = 150,
                    Margin = new Avalonia.Thickness(0, 0, 8, 12),
                    Spacing = 4,
                    Children =
                    {
                        sizes,
                        new TextBlock { Text = label, Classes = { "caption" }, HorizontalAlignment = HorizontalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis },
                    },
                });
            }

            sheet.Children.Add(wrap);
        }

        sheet.VerticalAlignment = VerticalAlignment.Top;
        var window = new Window { Width = 1300, Height = 1400, Content = sheet };
        window.Show();
        Settle();
        SaveCrop(window, sheet, name);
        window.Close();
    }
}
