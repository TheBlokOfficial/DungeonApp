using System.Collections.Generic;

namespace DungeonApp.Core.Content;

/// <summary>
/// Everything this build knows about installed packs, once <see cref="ContentPackLoader"/> has
/// scanned and validated them. Read-only and built exactly once per load - there is no way to add a
/// pack to an existing registry, mirroring <see cref="DataBlocks.DataBlockRegistry"/>'s stance that
/// what a build knows about is fixed for the life of that build (packs are re-scanned by loading a
/// fresh registry, not by mutating this one).
/// </summary>
/// <param name="SystemPacks">Every layer-2 pack that loaded and validated cleanly.</param>
/// <param name="ContentPacks">Every layer-3 pack that loaded and validated cleanly.</param>
/// <param name="Entries">
/// Every entry from every pack in <see cref="ContentPacks"/> - both the ones bound to a template and
/// the ones left unresolved. A caller that only wants what can actually render filters on
/// <see cref="RegisteredEntry.Template"/> being non-null; a caller building a "what needs attention"
/// view wants the unresolved ones just as much.
/// </param>
/// <param name="RejectedPacks">Every pack directory the loader refused, and why.</param>
public sealed record ContentRegistry(
    IReadOnlyList<SystemPack> SystemPacks,
    IReadOnlyList<ContentPack> ContentPacks,
    IReadOnlyList<RegisteredEntry> Entries,
    IReadOnlyList<RejectedPack> RejectedPacks);
