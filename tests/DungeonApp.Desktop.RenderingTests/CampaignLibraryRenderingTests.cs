using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using DungeonApp.Core.Campaigns;
using DungeonApp.Library.Entries;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Features.CampaignLibrary;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Headless-rendering coverage for the shelf row - specifically, the dimming an unavailable row's
/// name, date and arrow are meant to carry. A visible-value assertion (Bounds, Text) cannot catch this
/// class of bug the way <see cref="GlobalSidebarRenderingTests"/> catches a zero-bounds row: an
/// undimmed row draws exactly the right text in exactly the right place, just at full opacity instead
/// of the theme's own disabled-state opacity, and nothing but reading the rendered
/// <see cref="Avalonia.Visual.Opacity"/> back can tell the two apart.
/// </summary>
public sealed class CampaignLibraryRenderingTests
{
    [AvaloniaFact]
    public void An_unavailable_rows_name_date_and_arrow_render_dimmer_than_an_available_rows()
    {
        var window = BuildWindow();

        var availableRow = FindRow(window, "Dostępna");
        var unavailableRow = FindRow(window, "Niedostępna");

        var availableDim = EffectiveOpacity(availableRow);
        var unavailableDim = EffectiveOpacity(unavailableRow);

        Assert.Equal(1d, availableDim.Name, precision: 3);
        Assert.Equal(1d, availableDim.Date, precision: 3);
        Assert.Equal(1d, availableDim.Arrow, precision: 3);

        Assert.True(unavailableDim.Name < 1d, $"Nazwa niedostępnej kampanii ma pełną nieprzygaszoną krycie: {unavailableDim.Name}.");
        Assert.True(unavailableDim.Date < 1d, $"Data niedostępnej kampanii ma pełne nieprzygaszone krycie: {unavailableDim.Date}.");
        Assert.True(unavailableDim.Arrow < 1d, $"Strzałka niedostępnej kampanii ma pełne nieprzygaszone krycie: {unavailableDim.Arrow}.");
    }

    [AvaloniaFact]
    public void An_unavailable_rows_icon_and_trash_are_not_dimmed()
    {
        var window = BuildWindow();

        var unavailableRow = FindRow(window, "Niedostępna");

        var warningIcon = unavailableRow.GetVisualDescendants()
            .OfType<Image>()
            .First(image => image.IsVisible);
        var trashButton = unavailableRow.GetVisualDescendants()
            .OfType<Button>()
            .First(button => button.Classes.Contains("campaign-delete"));

        Assert.Equal(1d, EffectiveOpacityOf(warningIcon), precision: 3);
        Assert.Equal(1d, EffectiveOpacityOf(trashButton), precision: 3);
    }

    private static (double Name, double Date, double Arrow) EffectiveOpacity(Border row)
    {
        var name = row.GetVisualDescendants().OfType<TextBlock>().First(tb => tb.Classes.Contains("campaign-name"));
        var date = row.GetVisualDescendants().OfType<TextBlock>()
            .First(tb => !tb.Classes.Contains("campaign-name") && tb.Classes.Contains("campaign-dim"));
        var arrow = row.GetVisualDescendants().OfType<Image>().First(image => image.Classes.Contains("campaign-dim"));

        return (EffectiveOpacityOf(name), EffectiveOpacityOf(date), EffectiveOpacityOf(arrow));
    }

    /// <summary>
    /// Avalonia composites a visual's rendered opacity from its own <see cref="Avalonia.Visual.Opacity"/>
    /// together with every ancestor's, down to (not including) the row's own Border - multiplying the
    /// chain here is what makes this assertion catch dimming applied to an ancestor the element itself
    /// never carries, not only dimming set directly on the element.
    /// </summary>
    private static double EffectiveOpacityOf(Avalonia.Visual visual)
    {
        var opacity = 1d;

        for (Avalonia.Visual? current = visual; current is not null; current = current.GetVisualParent())
        {
            opacity *= current.Opacity;

            if (current is Border border && border.Classes.Contains("campaign-row"))
            {
                break;
            }
        }

        return opacity;
    }

    private static Border FindRow(Window window, string name)
    {
        var textBlock = window.GetVisualDescendants()
            .OfType<TextBlock>()
            .First(tb => tb.Classes.Contains("campaign-name") && tb.Text == name);

        return textBlock.GetVisualAncestors().OfType<Border>().First(border => border.Classes.Contains("campaign-row"));
    }

    private static Window BuildWindow()
    {
        var repository = new NoopCampaignRepository();
        IReadOnlyList<IGameSystem> systems = [];
        var preparations = new CampaignPreparationCache(repository, systems);
        var createCampaign = new CreateCampaign(repository, TimeProvider.System);

        var viewModel = new CampaignLibraryViewModel(repository, createCampaign, preparations, systems, _ => Task.CompletedTask);

        viewModel.Campaigns.Add(new CampaignRowViewModel(
            new CampaignSummary(CampaignId.New(), CampaignName.Create("Dostępna"), DateTimeOffset.UtcNow, null),
            CampaignAvailability.Available,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask));

        viewModel.Campaigns.Add(new CampaignRowViewModel(
            new CampaignSummary(CampaignId.New(), CampaignName.Create("Niedostępna"), DateTimeOffset.UtcNow, null),
            CampaignAvailability.NoSystem,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask));

        var view = new CampaignLibraryView { DataContext = viewModel };
        var window = new Window { Content = view, Width = 900, Height = 700 };
        window.Show();
        window.GetLayoutManager()!.ExecuteLayoutPass();

        return window;
    }

    /// <summary>Never actually called - the view model's rows are built by hand above, never through <see cref="CampaignLibraryViewModel.LoadAsync"/>.</summary>
    private sealed class NoopCampaignRepository : ICampaignRepository
    {
        public Task SaveAsync(Campaign campaign, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Campaign?> GetAsync(CampaignId id, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(CampaignId id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
