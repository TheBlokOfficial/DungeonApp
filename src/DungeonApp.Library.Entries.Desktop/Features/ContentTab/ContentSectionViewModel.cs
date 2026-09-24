using System.Collections.Generic;

namespace DungeonApp.Library.Entries.Desktop.Features.ContentTab;

/// <summary>
/// One section of a content tab's list, mirroring <see cref="ContentSection"/>: everything from one
/// pack, under a header naming that pack and how many rows currently show under it.
/// <para>
/// A rejected pack's section carries no <see cref="Rows"/> at all - <see cref="HeaderRow"/> is the
/// section, drawn like any other row (broken-content color, selectable) rather than a group label
/// (docs/architecture.md, "Zakładki treści": "Odrzucona paczka — ostatni nagłówek listy, bez
/// wpisów... wybieralny"). An ordinary pack section is the opposite: <see cref="HeaderRow"/> is null,
/// and the header is a plain, unselectable group label.
/// </para>
/// </summary>
public sealed class ContentSectionViewModel(string header, int count, ContentRowViewModel? headerRow, IReadOnlyList<ContentRowViewModel> rows)
{
    public string Header { get; } = header;

    public int Count { get; } = count;

    public bool IsRejectedPack => HeaderRow is not null;

    public ContentRowViewModel? HeaderRow { get; } = headerRow;

    public IReadOnlyList<ContentRowViewModel> Rows { get; } = rows;
}
