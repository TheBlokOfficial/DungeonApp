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
/// <para>
/// <paramref name="availability"/> is this row's whole verdict - computed once, by
/// <see cref="CampaignPreparationCache.CheckAvailabilityAsync"/>, before the row is even built.
/// A row whose <see cref="Availability"/> is not <see cref="CampaignAvailability.Available"/> shows a
/// reason (<see cref="UnavailabilityReason"/>) and refuses to open - <see cref="OpenCommand"/>'s own
/// <c>canExecute</c> is only half of that refusal; the operation itself refuses again, from
/// <see cref="CampaignPreparationCache.TakeAsync"/>, docs/architecture.md's "Kampania należy do
/// jednego systemu".
/// </para>
/// </summary>
public sealed class CampaignRowViewModel
{
    public CampaignRowViewModel(
        CampaignSummary summary,
        CampaignAvailability availability,
        Func<CampaignRowViewModel, Task> open,
        Func<CampaignRowViewModel, Task> delete)
    {
        Summary = summary;
        Id = summary.Id;
        Name = summary.Name.Value;
        // Stored in UTC, read by a person sitting in their own timezone.
        CreatedAt = summary.CreatedAt.ToLocalTime().ToString("d MMM yyyy", CultureInfo.CurrentCulture);
        Availability = availability;
        UnavailabilityReason = DescribeUnavailability(availability, summary);
        OpenCommand = new AsyncCommand(() => open(this), () => IsAvailable);
        DeleteCommand = new AsyncCommand(() => delete(this));
    }

    /// <summary>What <see cref="CampaignPreparationCache"/> needs to open, peek at or check this campaign again.</summary>
    public CampaignSummary Summary { get; }

    public CampaignId Id { get; }

    public string Name { get; }

    public string CreatedAt { get; }

    public CampaignAvailability Availability { get; }

    public bool IsAvailable => Availability == CampaignAvailability.Available;

    /// <summary>Null when <see cref="IsAvailable"/> - there is nothing to explain about a campaign that opens fine.</summary>
    public string? UnavailabilityReason { get; }

    public ICommand OpenCommand { get; }

    public ICommand DeleteCommand { get; }

    private static string? DescribeUnavailability(CampaignAvailability availability, CampaignSummary summary) => availability switch
    {
        CampaignAvailability.Available => null,
        CampaignAvailability.NoSystem =>
            "Kampania bez przypisanego systemu — zapisana starszą wersją programu.",
        CampaignAvailability.UnknownSystem =>
            $"System „{summary.SystemId}” nie jest dostępny w tym programie.",
        CampaignAvailability.NewerFormat => "Zapisana nowszą wersją programu.",
        CampaignAvailability.IncompatibleModelVersion => "Niezgodna wersja danych systemu.",
        CampaignAvailability.Corrupted => "Pliki kampanii są uszkodzone.",
        _ => throw new ArgumentOutOfRangeException(nameof(availability), availability, null)
    };
}
