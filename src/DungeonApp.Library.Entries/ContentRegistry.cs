using System.Collections.Generic;

namespace DungeonApp.Library.Entries;

/// <summary>
/// Everything this build knows about installed packs, once <see cref="ContentPackLoader"/> has
/// scanned and validated them. Read-only and built exactly once per load - there is no way to add a
/// pack to an existing registry: what a build knows about is fixed for the life of that build
/// (packs are re-scanned by loading a fresh registry, not by mutating this one).
/// </summary>
/// <param name="Packs">Every pack that loaded and validated cleanly.</param>
/// <param name="Entries">
/// Every entry from every pack in <see cref="Packs"/> - both the ones bound to a content type and
/// the ones left unresolved. A caller that only wants what can actually render filters on
/// <see cref="RegisteredEntry.Type"/> being non-null; a caller building a "what needs attention"
/// view wants the unresolved ones just as much.
/// </param>
/// <param name="RejectedEntries">
/// Every file in an accepted pack's <c>entries</c> directory that could not be read as an entry at
/// all - not valid JSON, an unknown key, an invalid id, a duplicated id. Distinct from
/// <see cref="Entries"/> carrying an unresolved <see cref="RegisteredEntry"/>, which is a file that
/// parsed fine but could not be bound to a content type.
/// </param>
/// <param name="RejectedPacks">Every pack directory the loader refused, and why.</param>
public sealed record ContentRegistry(
    IReadOnlyList<Pack> Packs,
    IReadOnlyList<RegisteredEntry> Entries,
    IReadOnlyList<RejectedEntry> RejectedEntries,
    IReadOnlyList<RejectedPack> RejectedPacks);
