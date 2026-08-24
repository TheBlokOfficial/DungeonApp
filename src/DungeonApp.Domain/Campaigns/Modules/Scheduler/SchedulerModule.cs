using DungeonApp.Domain.Campaigns.Events;
using DungeonApp.Domain.Campaigns.Modules.Clock;

namespace DungeonApp.Domain.Campaigns.Modules.Scheduler;

/// <summary>
/// Optional module that turns world-time advancement into due, auditable world events.
/// </summary>
public sealed class SchedulerModule : ICampaignModule, ICampaignModuleDependencyDeclaration
{
    private readonly List<ScheduledWorldEvent> _scheduledEvents;

    public static ModuleId Id { get; } = new("core.scheduler");

    public SchedulerModule(IEnumerable<ScheduledWorldEvent>? scheduledEvents = null)
    {
        _scheduledEvents = scheduledEvents?.ToList() ?? [];

        if (_scheduledEvents.GroupBy(scheduledEvent => scheduledEvent.Id).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Scheduled event ids must be unique.", nameof(scheduledEvents));
        }
    }

    public CampaignModuleDescriptor Descriptor { get; } = new(Id, stateVersion: 1);

    public IReadOnlyCollection<ModuleId> RequiredModuleIds { get; } = [ClockModule.Id];

    public IReadOnlyList<ScheduledWorldEvent> ScheduledEvents => _scheduledEvents
        .OrderBy(scheduledEvent => scheduledEvent.DueAt)
        .ThenBy(scheduledEvent => scheduledEvent.Id)
        .ToList()
        .AsReadOnly();

    public ICampaignModule CreateWorkingCopy() => new SchedulerModule(_scheduledEvents);

    public void Handle(CampaignCommand command, CampaignModuleContext context)
    {
        if (command is not ScheduleWorldEvent scheduleWorldEvent)
        {
            throw new InvalidOperationException($"Scheduler module cannot handle '{command.GetType().Name}'.");
        }

        var scheduledEvent = new ScheduledWorldEvent(
            Guid.NewGuid(),
            context.CurrentTime.AdvanceBy(scheduleWorldEvent.Delay),
            scheduleWorldEvent.Title,
            scheduleWorldEvent.Reason);
        context.Publish(new WorldEventScheduled(scheduledEvent), context.CurrentTime);
    }

    public void ReactTo(CampaignEvent campaignEvent, CampaignModuleContext context)
    {
        switch (campaignEvent.Payload)
        {
            case WorldEventScheduled eventScheduled:
                Add(eventScheduled.ScheduledEvent);
                break;

            case WorldTimeAdvanced timeAdvanced:
                RaiseDueEvents(timeAdvanced, context);
                break;
        }
    }

    private void Add(ScheduledWorldEvent scheduledEvent)
    {
        if (_scheduledEvents.Any(existing => existing.Id == scheduledEvent.Id))
        {
            throw new InvalidOperationException($"Scheduled event '{scheduledEvent.Id}' already exists.");
        }

        _scheduledEvents.Add(scheduledEvent);
    }

    private void RaiseDueEvents(WorldTimeAdvanced timeAdvanced, CampaignModuleContext context)
    {
        var dueEvents = _scheduledEvents
            .Where(scheduledEvent =>
                scheduledEvent.DueAt.CompareTo(timeAdvanced.From) > 0 &&
                scheduledEvent.DueAt.CompareTo(timeAdvanced.To) <= 0)
            .OrderBy(scheduledEvent => scheduledEvent.DueAt)
            .ThenBy(scheduledEvent => scheduledEvent.Id)
            .ToList();

        foreach (var dueEvent in dueEvents)
        {
            _scheduledEvents.Remove(dueEvent);
            context.Publish(new ScheduledWorldEventDue(dueEvent), dueEvent.DueAt);
        }
    }
}
