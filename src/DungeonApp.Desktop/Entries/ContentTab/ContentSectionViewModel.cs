using System.Collections.Generic;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries.ContentTab;

/// <summary>
/// One section of a content tab's list, mirroring <see cref="ContentSection"/>: everything from one
/// pack, under a header naming that pack and how many rows currently show under it.
/// <para>
/// A rejected pack's section is drawn the same way - an unselectable header - but in the
/// broken-content color with a warning icon, and its <see cref="Rows"/> hold exactly one row: the
/// pack's own directory, selectable like any other row, whose detail gives the reason
/// (docs/architecture.md, "Zakładki treści").
/// </para>
/// </summary>
public sealed class ContentSectionViewModel(string header, int count, bool isRejectedPack, IReadOnlyList<ContentRowViewModel> rows)
{
    public string Header { get; } = header;

    /// <summary>Loaded and broken rows shown under this header; not shown for a rejected pack.</summary>
    public int Count { get; } = count;

    public bool IsRejectedPack { get; } = isRejectedPack;

    public IReadOnlyList<ContentRowViewModel> Rows { get; } = rows;
}
