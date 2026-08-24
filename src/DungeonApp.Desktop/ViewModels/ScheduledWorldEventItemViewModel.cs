using System;
using DungeonApp.Application.Campaigns;

namespace DungeonApp.Desktop.ViewModels;

public sealed class ScheduledWorldEventItemViewModel(ScheduledWorldEventInfo scheduledEvent)
{
    public string Title => scheduledEvent.Title;

    public string Reason => scheduledEvent.Reason;

    public string DueAt => $"Termin: {scheduledEvent.DueAt.Days}d {scheduledEvent.DueAt.Hours}h {scheduledEvent.DueAt.Minutes}m";
}
