using System;
using System.Collections.Generic;
using System.Globalization;

namespace DungeonApp.Core.Modules.Clock;

/// <summary>
/// How much has passed since the campaign began. Deliberately not a calendar date: the core knows
/// only elapsed time, and a ruleset is free to present it later as its own days, months and seasons.
/// <para>
/// The unit is the whole second, which is the finest granularity any tabletop rule needs - a combat
/// round is measured in seconds, never in fractions of one.
/// </para>
/// </summary>
public readonly record struct CampaignTime
{
    private CampaignTime(long totalSeconds) => TotalSeconds = totalSeconds;

    public static CampaignTime Start => new(0);

    public long TotalSeconds { get; }

    public TimeSpan Elapsed => TimeSpan.FromSeconds(TotalSeconds);

    public static CampaignTime FromSeconds(long totalSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalSeconds);

        return new CampaignTime(totalSeconds);
    }

    /// <summary>
    /// Whether an amount can be added at all. Asked by the clock before it changes anything, so the
    /// refusal is a rule the GM reads rather than an exception from deep inside a value type.
    /// </summary>
    public static bool IsWholeSeconds(TimeSpan amount) => amount.Ticks % TimeSpan.TicksPerSecond == 0;

    public CampaignTime Add(TimeSpan amount)
    {
        if (!IsWholeSeconds(amount))
        {
            throw new ArgumentException("World time moves in whole seconds.", nameof(amount));
        }

        return FromSeconds(checked(TotalSeconds + (long)amount.TotalSeconds));
    }

    /// <summary>
    /// A short reading for the chronicle: the two most significant units that carry anything, so
    /// three days and a quarter hour reads as "3 d 15 min" and not as a run of zeroes.
    /// </summary>
    public static string Describe(TimeSpan amount)
    {
        var parts = new (long Value, string Unit)[]
        {
            ((long)amount.TotalDays, "d"),
            (amount.Hours, "h"),
            (amount.Minutes, "min"),
            (amount.Seconds, "s")
        };

        var written = new List<string>(2);

        foreach (var (value, unit) in parts)
        {
            if (value != 0 && written.Count < 2)
            {
                written.Add($"{value.ToString(CultureInfo.CurrentCulture)} {unit}");
            }
        }

        return written.Count > 0 ? string.Join(' ', written) : "0 s";
    }

    public override string ToString() => Describe(Elapsed);
}
