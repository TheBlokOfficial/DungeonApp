using System;
using DungeonApp.Core.Modules;

namespace DungeonApp.Core.Journal;

/// <summary>
/// One line of the campaign chronicle: what changed, when, and on what grounds.
/// <para>
/// The journal answers the GM's question "why is the world like this", and nothing more. It is
/// never replayed to rebuild state - the module state files are the truth - which is what lets it
/// grow without bound, live outside the manifest, and survive being damaged.
/// </para>
/// </summary>
/// <param name="Module">
/// Which module changed something. Null means the GM acted directly: a manual correction is a
/// recorded event, never a silent way around the rules.
/// </param>
public sealed record JournalEntry(
    DateTimeOffset RecordedAt,
    ModuleId? Module,
    string Summary,
    string? Reason);
