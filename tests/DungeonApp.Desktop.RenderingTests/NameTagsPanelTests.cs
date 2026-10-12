using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>The name-and-tags row: the name keeps its natural width when there is room, even after it widens.</summary>
public sealed class NameTagsPanelTests
{
    [AvaloniaFact]
    public void A_name_that_turns_bold_is_not_trimmed_while_there_is_room()
    {
        var name = new TextBlock { Text = "Rycerz", TextTrimming = TextTrimming.CharacterEllipsis };
        var tags = new TextBlock { Text = "Średni, humanoid (dowolna rasa), dowolny charakter", TextTrimming = TextTrimming.CharacterEllipsis };
        var panel = new NameTagsPanel { Spacing = 8, Width = 600 };
        panel.Children.Add(name);
        panel.Children.Add(tags);
        var window = new Window { Width = 800, Height = 200, Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        name.FontWeight = FontWeight.Bold;
        Dispatcher.UIThread.RunJobs();
        var bold = new TextBlock { Text = name.Text, FontWeight = FontWeight.Bold, FontFamily = name.FontFamily, FontSize = name.FontSize };
        bold.Measure(Avalonia.Size.Infinity);
        var natural = bold.DesiredSize.Width;

        Assert.True(name.Bounds.Width >= natural - 0.5, $"name {name.Bounds.Width} < natural {natural}");

        window.Close();
    }
}
