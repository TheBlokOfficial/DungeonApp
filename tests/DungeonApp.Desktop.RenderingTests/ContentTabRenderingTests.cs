using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
    public void The_selected_rows_stripe_is_a_separate_element_inset_at_the_rows_own_left_edge()
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
        // krok 10, brief A5: every negative offset tried here (the original -10, and -6, -2)
        // rendered invisible or clipped to a sliver - see
        // Selected_rows_stripe_renders_a_visible_pixel_at_its_own_center below, and this type's own
        // remarks. The stripe sits at the row's own left edge (X equal, not offset) instead.
        Assert.Equal(rowTopLeft.X, stripeTopLeft.X);
        Assert.Equal(rowTopLeft.Y + 5, stripeTopLeft.Y);
        Assert.Equal(row.Bounds.Height - 10, stripe.Bounds.Height);
    }

    /// <summary>
    /// krok 10, brief A5: "Kreska zaznaczenia wiersza listy musi być widoczna. Dziś przycina ją
    /// krawędź listy." A <see cref="Control.Bounds"/> assertion (the test above) cannot catch a
    /// clipped-to-invisible element - its Bounds are exactly what they should be either way, since
    /// clipping happens during rendering, not layout. Only an actual rendered pixel, sampled from the
    /// whole window (reproducing the exact clip/paint chain a GM actually sees), tells the two apart.
    /// <para>
    /// This fails against the original -10 margin (ContentRowSelectionStripeMargin,
    /// ContentTabView.axaml): with ContentListScrollPadding's own 10px left padding, -10 lands the
    /// stripe exactly on the row's own left edge and paints nothing there - measured directly (see
    /// that resource's own remarks) - and every other negative offset tried behaved the same way, all
    /// the way down to -2.
    /// </para>
    /// </summary>
    [AvaloniaFact]
    public void Selected_rows_stripe_renders_a_visible_pixel_at_its_own_center()
    {
        var viewModel = BuildViewModel();
        var window = BuildWindow(width: 1920, viewModel);

        viewModel.Sections.Single().Rows.Single().SelectCommand.Execute(null);
        window.GetLayoutManager()!.ExecuteLayoutPass();

        var stripe = window.GetVisualDescendants().OfType<Border>()
            .Single(b => b.Classes.Contains("content-row-stripe") && b.IsVisible);

        // A point well inside the stripe's own bounds (past its rounded corners), in the WHOLE
        // window's own coordinate space.
        var samplePoint = stripe.TranslatePoint(new Point(1.25, stripe.Bounds.Height / 2), window)!.Value;

        var pixel = SamplePixel(window, samplePoint);
        var accent = ResolveColor(window, "DungeonAccentBrush");

        // The stripe brush itself is fully opaque (Background="{DynamicResource DungeonAccentBrush}"),
        // so a visible stripe pixel matches the accent color outright - a clipped one instead shows
        // whatever sits behind it (the list's own background), never this color.
        Assert.Equal(accent, pixel);
    }

    /// <summary>krok 10, brief A5: "Zaznaczony wiersz pogrubiony."</summary>
    [AvaloniaFact]
    public void Selecting_a_row_makes_its_name_semibold_and_deselecting_it_returns_to_normal()
    {
        var viewModel = BuildViewModel();
        var window = BuildWindow(width: 1920, viewModel);

        // Re-queried after selecting, not captured beforehand: selecting rebuilds Sections (a fresh
        // ContentRowViewModel/ContentSectionViewModel list, per ContentTabViewModel's own Apply/
        // ApplyResult), so the ItemsControl regenerates its row containers - a TextBlock reference
        // captured before the rebuild would be stale, detached from the visual tree.
        Assert.Equal(FontWeight.Normal, CurrentRowName(window).FontWeight);

        viewModel.Sections.Single().Rows.Single().SelectCommand.Execute(null);
        window.GetLayoutManager()!.ExecuteLayoutPass();

        Assert.Equal(FontWeight.SemiBold, CurrentRowName(window).FontWeight);
    }

    private static Color SamplePixel(Visual visual, Point point)
    {
        var width = (int)Math.Ceiling(visual.Bounds.Width);
        var height = (int)Math.Ceiling(visual.Bounds.Height);

        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height));
        bitmap.Render(visual);

        var buffer = Marshal.AllocHGlobal(4);
        try
        {
            var samplePoint = new PixelPoint((int)point.X, (int)point.Y);
            bitmap.CopyPixels(new PixelRect(samplePoint.X, samplePoint.Y, 1, 1), buffer, 4, 4);

            var bytes = new byte[4];
            Marshal.Copy(buffer, bytes, 0, 4);

            // Avalonia's default software render target pixel format is Bgra8888.
            return Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static Color ResolveColor(Window window, string brushResourceKey)
    {
        var brush = (ISolidColorBrush)window.FindResource(brushResourceKey)!;
        return brush.Color;
    }

    private static TextBlock CurrentRowName(Window window) =>
        window.GetVisualDescendants().OfType<TextBlock>().Single(tb => tb.Classes.Contains("row-name") && tb.IsVisible);

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
