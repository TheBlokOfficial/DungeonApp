using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Events;
using DungeonApp.Core.Modules.Clock;

namespace DungeonApp.Core.Modules.Scheduler;

/// <summary>
/// Something the GM set to happen at a point in world time, and that has now arrived.
/// </summary>
public sealed record ScheduledWorldEventDue(Guid Id, string Name, CampaignTime DueAt) : ICampaignEvent;

/// <summary>Something waiting to happen. Identity is stable so it survives saves and reorderings.</summary>
public sealed record ScheduledWorldEvent(Guid Id, long DueAtSeconds, string Name);

/// <summary>What the scheduler keeps between sessions.</summary>
public sealed record SchedulerState(IReadOnlyList<ScheduledWorldEvent> Pending);

/// <summary>
/// Keeps a list of things due at a point in world time and announces them when that point passes.
/// <para>
/// The second module, and the one that decided the shape of the announcement channel. It uses both
/// halves of it, which is the whole point: it <em>asks</em> the clock what time it is through the
/// clock's own interface, and it <em>hears</em> that time moved as an announcement. A question and a
/// fact are different things and travel differently.
/// </para>
/// </summary>
public sealed class SchedulerModule : ICampaignModule
{
    public static ModuleId Id { get; } = ModuleId.Create("core.scheduler");

    private readonly List<ScheduledWorldEvent> _pending = [];

    private ModuleContext? _context;
    private ClockModule? _clock;

    /// <summary>
    /// The dependency is declared, not discovered: a campaign that switches the scheduler on without
    /// a clock is refused when it is assembled rather than failing the first time time moves.
    /// </summary>
    public ModuleManifest Manifest { get; } =
        new(Id, "Harmonogram świata", StateVersion: 1, Requires: [ClockModule.Id]);

    public IReadOnlyList<ScheduledWorldEvent> Pending => _pending;

    public void OnActivated(ModuleContext context)
    {
        _context = context;

        // Resolved directly, because "what time is it" is a question and questions are not
        // announcements. Safe here: every module is registered before any is activated.
        _clock = context.Modules.Get<ClockModule>();

        context.Events.Subscribe<WorldTimeAdvanced>(OnWorldTimeAdvanced);
    }

    /// <summary>
    /// Puts something on the calendar, a stated distance from now. Everything that can be refused is
    /// refused before the list is touched.
    /// </summary>
    public Guid Schedule(TimeSpan delay, string name, string reason)
    {
        if (_context is null || _clock is null)
        {
            throw new InvalidOperationException("The scheduler was used before its campaign activated it.");
        }

        if (delay <= TimeSpan.Zero)
        {
            throw new CampaignRuleException("Zdarzenie można zaplanować wyłącznie w przyszłości.");
        }

        if (!CampaignTime.IsWholeSeconds(delay))
        {
            throw new CampaignRuleException("Najmniejszą jednostką czasu świata jest sekunda.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new CampaignRuleException("Zaplanowane zdarzenie wymaga nazwy.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new CampaignRuleException("Zaplanowanie zdarzenia wymaga podania powodu.");
        }

        var dueAt = _clock.Now.Add(delay);
        var scheduled = new ScheduledWorldEvent(Guid.NewGuid(), dueAt.TotalSeconds, name.Trim());

        _pending.Add(scheduled);

        _context.Journal.Record(
            $"Zaplanowano „{scheduled.Name}” za {CampaignTime.Describe(delay)}",
            reason.Trim());

        return scheduled.Id;
    }

    /// <summary>
    /// One move of the clock can bring several things due at once, so they are taken in due order
    /// and, for a tie, in the order they were scheduled. The same save must resolve the same way.
    /// </summary>
    private void OnWorldTimeAdvanced(WorldTimeAdvanced announcement)
    {
        var due = _pending
            .Where(scheduled => scheduled.DueAtSeconds <= announcement.Now.TotalSeconds)
            .OrderBy(scheduled => scheduled.DueAtSeconds)
            .ToArray();

        foreach (var scheduled in due)
        {
            // Removed before announcing: a listener that moves the clock again must not find this
            // one still waiting and fire it twice.
            _pending.Remove(scheduled);

            _context!.Journal.Record(
                $"Nadszedł termin: „{scheduled.Name}”",
                $"upływ czasu świata (+{CampaignTime.Describe(announcement.Amount)})");

            _context.Events.Publish(new ScheduledWorldEventDue(
                scheduled.Id, scheduled.Name, CampaignTime.FromSeconds(scheduled.DueAtSeconds)));
        }
    }

    public Type StateType => typeof(SchedulerState);

    public object CaptureState() => new SchedulerState([.. _pending]);

    public void RestoreState(object state, int version)
    {
        _pending.Clear();
        _pending.AddRange(((SchedulerState)state).Pending ?? []);
    }
}
