using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignLibrary;

/// <summary>
/// One row on the shelf. Built from a <see cref="CampaignSummary"/>, never from a loaded campaign:
/// drawing the list must not cost the state of every campaign in it.
/// </summary>
public sealed class CampaignRowViewModel
{
    public CampaignRowViewModel(
        CampaignSummary summary,
        Func<CampaignRowViewModel, Task> open,
        Func<CampaignRowViewModel, Task> delete)
    {
        Id = summary.Id;
        Name = summary.Name.Value;
        // Stored in UTC, read by a person sitting in their own timezone.
        CreatedAt = summary.CreatedAt.ToLocalTime().ToString("d MMM yyyy", CultureInfo.CurrentCulture);
        OpenCommand = new AsyncCommand(() => open(this));
        DeleteCommand = new AsyncCommand(() => delete(this));
    }

    public CampaignId Id { get; }

    public string Name { get; }

    public string CreatedAt { get; }

    public ICommand OpenCommand { get; }

    public ICommand DeleteCommand { get; }
}
