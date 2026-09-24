using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using DungeonApp.Library.Entries;
using DungeonApp.Library.Entries.Desktop.Content;
using DungeonApp.Library.Entries.Desktop.Features.ContentTab;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Headless-rendering coverage for the content tab skeleton's layout (krok 10, zlecenie 2 brief):
/// docs/images/mockup_rejestr.html's own list-panel width (320) and detail-inner max-width (720),
/// and the selected-row look the coordinator's mid-task clarification pinned down precisely - a dim
/// background plus a SEPARATE accent stripe, offset -10px/inset 5px/width 2.5px/radius 2px, never a
/// border on the row itself. Every assertion below is an equality, the same discipline
/// <see cref="TopBarRenderingTests"/> already follows, and measures against the row's own bounds
/// rather than a hard-coded window size so it keeps catching the same class of mistake if the
/// tokens change.
/// </summary>
public sealed class ContentTabRenderingTests
{
    private static readonly ContentId TestSet = ContentId.Create("test");
    private static readonly ContentTypeReference WidgetType = new(TestSet, ContentId.Create("widget"));

    private sealed record Widget(string Tier);

    private sealed class FakePresentation : IContentPresentation
    {
        public Control CreateCard(Entry entry) => new TextBlock { Text = entry.Name };

        public IBrush? ResolveBadgeBrush(string colorKey) => null;
    }

    private static ContentTabDefinition WidgetTab() => new(
        "Widgety", [new ContentTypeProfile<Widget>(WidgetType)]);

    private static Entry MakeEntry(string id, string name) =>
        new(ContentId.Create(id), name, WidgetType, TypeVersion: 1, ContentValues.From(new Widget("1")));

    private static RegisteredEntry Valid(string pack, string id, string name) =>
        RegisteredEntry.CreateResolved(
            new EntryAddress(ContentId.Create(pack), ContentId.Create(id)),
            MakeEntry(id, name),
            new ContentTypeDescriptor(WidgetType, "Widget", 1));

    private static ContentTabViewModel BuildViewModel()
    {
        var pack = new Pack(ContentId.Create("p"), "Pack", new PackVersion(1, 0), [MakeEntry("a", "Alpha")]);
        var registry = new ContentRegistry([pack], [Valid("p", "a", "Alpha")], [], []);

        return new ContentTabViewModel(registry, WidgetTab(), [WidgetType], new FakePresentation());
    }

    // -----------------------------------------------------------------------------------------
    // Column widths: 1920 (16:9, wide) and 1040 (list 320 + detail 720 - the mockup's own layout,
    // with nothing left over).
    // -----------------------------------------------------------------------------------------

    [AvaloniaFact]
    public void At_1920_the_list_column_and_detail_content_keep_the_mockups_widths_with_room_left_over()
    {
        var window = BuildWindow(width: 1920);

        var listColumn = FindByName<Border>(window, "PART_ListColumn");
        var detailInner = FindByName<Border>(window, "PART_DetailInner");

        Assert.Equal(320, listColumn.Bounds.Width);
        Assert.Equal(720, detailInner.Bounds.Width);
        // Neither column stretches to fill the window - what is left over (1920 - 320 - 720) stays
        // empty on the right, exactly docs/architecture.md's "nadmiar szerokości zostaje pusty po
        // prawej".
        Assert.Equal(1920 - 320 - 720, window.Bounds.Width - listColumn.Bounds.Width - detailInner.Bounds.Width);
    }

    [AvaloniaFact]
    public void At_the_mockups_own_width_the_list_and_detail_columns_exactly_fill_it()
    {
        var window = BuildWindow(width: 320 + 720);

        var listColumn = FindByName<Border>(window, "PART_ListColumn");
        var detailInner = FindByName<Border>(window, "PART_DetailInner");

        Assert.Equal(320, listColumn.Bounds.Width);
        Assert.Equal(720, detailInner.Bounds.Width);
        Assert.Equal(window.Bounds.Width, listColumn.Bounds.Width + detailInner.Bounds.Width);
    }

    // -----------------------------------------------------------------------------------------
    // Selected row: dim background plus a separate stripe (architect's mid-task clarification).
    // -----------------------------------------------------------------------------------------

    [AvaloniaFact]
    public void The_selected_rows_stripe_is_a_separate_element_offset_from_the_row_with_a_gap()
    {
        var viewModel = BuildViewModel();
        var window = BuildWindow(width: 1920, viewModel);

        // Selecting through the view model, not a click, keeps this test about geometry, not input.
        viewModel.Sections.Single().Rows.Single().SelectCommand.Execute(null);
        window.GetLayoutManager()!.ExecuteLayoutPass();

        var row = window.GetVisualDescendants().OfType<Border>()
            .Single(b => b.Classes.Contains("content-row-background") && b.Classes.Contains("selected"));
        var stripe = window.GetVisualDescendants().OfType<Border>()
            .Single(b => b.Classes.Contains("content-row-stripe") && b.IsVisible);

        var rowTopLeft = TopLeftIn(row, window);
        var stripeTopLeft = TopLeftIn(stripe, window);

        Assert.Equal(2.5, stripe.Bounds.Width);
        Assert.Equal(2, stripe.CornerRadius.TopLeft);
        Assert.Equal(rowTopLeft.X - 10, stripeTopLeft.X);
        Assert.Equal(rowTopLeft.Y + 5, stripeTopLeft.Y);
        Assert.Equal(row.Bounds.Height - 10, stripe.Bounds.Height);

        // The gap the coordinator called out: the stripe's right edge never touches the row's own
        // left edge - it stops short, with empty space between them.
        var gap = rowTopLeft.X - (stripeTopLeft.X + stripe.Bounds.Width);
        Assert.Equal(7.5, gap);
    }

    private static Point TopLeftIn(Visual visual, Visual ancestor) =>
        visual.TranslatePoint(new Point(0, 0), ancestor)!.Value;

    private static T FindByName<T>(Window window, string name) where T : Control =>
        window.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);

    private static Window BuildWindow(double width, ContentTabViewModel? viewModel = null)
    {
        var view = new ContentTabView { DataContext = viewModel ?? BuildViewModel() };
        // UseLayoutRounding defaults to on, which would snap the stripe's own 2.5px width (and the
        // 7.5px gap it leaves) to whole device pixels - off here so the geometry this test measures
        // is the exact mockup value, not a rounded one.
        var window = new Window { Content = view, Width = width, Height = 1000, UseLayoutRounding = false };
        window.Show();
        window.GetLayoutManager()!.ExecuteLayoutPass();

        return window;
    }
}
