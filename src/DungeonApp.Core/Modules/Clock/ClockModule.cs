using System;

namespace DungeonApp.Core.Modules.Clock;

/// <summary>
/// Keeps how much time the campaign has lived through. The first module, and the one that proves the
/// contract: it owns a scrap of state, refuses what the rules forbid, and writes down why the world
/// changed.
/// <para>
/// It answers questions directly rather than through announcements, because a module asking another
/// what time it is is asking a question, not listening for news.
/// </para>
/// </summary>
public sealed class ClockModule : ICampaignModule
{
    public static ModuleId Id { get; } = ModuleId.Create("core.clock");

    private ModuleContext? _context;
    private CampaignTime _now = CampaignTime.Start;

    public ModuleManifest Manifest { get; } = new(Id, "Zegar kampanii", StateVersion: 1, Requires: []);

    /// <summary>The direct, typed question other modules ask.</summary>
    public CampaignTime Now => _now;

    public void OnActivated(ModuleContext context) => _context = context;

    /// <summary>
    /// Moves the world forward. Everything that can be refused is refused before a single field
    /// changes, so a rejected command leaves the campaign exactly as it was and the chronicle
    /// records only what actually happened.
    /// </summary>
    public void Advance(TimeSpan amount, string reason)
    {
        if (_context is null)
        {
            throw new InvalidOperationException("The clock was used before its campaign activated it.");
        }

        if (amount <= TimeSpan.Zero)
        {
            throw new CampaignRuleException("Czas świata może płynąć wyłącznie naprzód.");
        }

        if (!CampaignTime.IsWholeSeconds(amount))
        {
            throw new CampaignRuleException("Najmniejszą jednostką czasu świata jest sekunda.");
        }

        // The world changes for a reason, and the reason is recorded. Without one there is nothing
        // for the chronicle to explain the change with.
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new CampaignRuleException("Przesunięcie czasu wymaga podania powodu.");
        }

        _now = _now.Add(amount);

        _context.Journal.Record(
            $"Czas świata: +{CampaignTime.Describe(amount)} (łącznie {CampaignTime.Describe(_now.Elapsed)})",
            reason.Trim());
    }

    public Type StateType => typeof(ClockState);

    public object CaptureState() => new ClockState(_now.TotalSeconds);

    public void RestoreState(object state, int version) =>
        _now = CampaignTime.FromSeconds(((ClockState)state).ElapsedSeconds);
}

/// <summary>
/// What the clock keeps between sessions. Seconds as a plain number rather than a duration string,
/// so the file stays legible to a person opening it in an editor.
/// </summary>
public sealed record ClockState(long ElapsedSeconds);
