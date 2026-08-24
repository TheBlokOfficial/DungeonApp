using System;
using DungeonApp.Application.Campaigns;

namespace DungeonApp.Desktop.ViewModels;

public sealed class HistoryItemViewModel(CampaignHistoryEntry entry)
{
    public string Sequence => $"#{entry.SequenceNumber}";

    public string Title => entry.Title;

    public string Description => entry.Description;

    public string OccurredAt => FormatTime(entry.OccurredAt);

    private static string FormatTime(TimeSpan time) => $"Czas świata: {time.Days}d {time.Hours}h {time.Minutes}m";
}
