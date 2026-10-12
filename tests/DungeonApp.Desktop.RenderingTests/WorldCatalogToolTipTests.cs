using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace DungeonApp.Desktop.RenderingTests;

public sealed partial class WorldCatalogViewBuildTests
{
    // A row name shows its tooltip only when it is drawn trimmed, at every display scale (layout
    // rounding differs per scale); a name too long for the panel gets the full text.
    [AvaloniaFact]
    public async Task A_row_name_has_a_tooltip_only_when_trimmed()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        var longName = string.Concat(Enumerable.Repeat("Bardzo długa nazwa ", 20)).Trim();
        await catalog.CreateFolderAsync(null, "Jaskinia");
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Goblin 10");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null, longName);
        fixture.Settle();

        foreach (var scale in new[] { 1.0, 1.25, 1.5, 1.75, 2.0 })
        {
            fixture.Window.SetRenderScaling(scale);
            fixture.Settle();

            var names = fixture.CatalogView.GetVisualDescendants().OfType<TextBlock>()
                .Where(text => text.IsEffectivelyVisible && text.Classes.Contains("world-name"))
                .ToDictionary(text => text.Text!);
            Assert.Null(ToolTip.GetTip(names["Świat"]));
            Assert.Null(ToolTip.GetTip(names["Jaskinia"]));
            Assert.Null(ToolTip.GetTip(names["Goblin 10"]));
            Assert.Equal(longName, ToolTip.GetTip(names[longName]));

            // Nothing above a row carries a tip of its own that the row would show instead.
            var row = fixture.RowBorders().First();
            Assert.All(row.GetSelfAndVisualAncestors().OfType<Control>(), control => Assert.Null(ToolTip.GetTip(control)));
        }
    }

    // The desk's leaf strip keeps disabled actions (undo, redo, commands): a disabled control never
    // shows its tooltip.
    [AvaloniaFact]
    public void No_disabled_control_on_the_desk_shows_a_tooltip()
    {
        using var fixture = new Fixture();

        var disabledWithTips = fixture.Window.GetVisualDescendants().OfType<Control>()
            .Where(control => ToolTip.GetTip(control) is not null && !control.IsEffectivelyEnabled)
            .ToList();
        Assert.NotEmpty(disabledWithTips);
        Assert.All(disabledWithTips, control => Assert.False(ToolTip.GetShowOnDisabled(control)));
    }
}
