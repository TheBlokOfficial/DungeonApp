using System.Collections.Generic;

namespace DungeonApp.Core.Entries;

/// <summary>
/// One section of a content tab's list: everything from one pack. A rejected pack's section never
/// carries a row - <see cref="RejectedPack"/> is the only thing it has to show, per
/// docs/architecture.md's "Zakładki treści": "Odrzucona paczka — ostatni nagłówek listy, bez
/// wpisów." The two factory methods are the only way to build one, the same either/or discipline
/// <see cref="RegisteredEntry"/> and <see cref="ContentBrokenRow"/> already follow.
/// </summary>
public sealed record ContentSection
{
    private ContentSection(
        string header, RejectedPack? rejectedPack, IReadOnlyList<RegisteredEntry> validRows, IReadOnlyList<ContentBrokenRow> brokenRows)
    {
        Header = header;
        RejectedPack = rejectedPack;
        ValidRows = validRows;
        BrokenRows = brokenRows;
    }

    /// <summary>The pack's own display name, or a rejected pack's location when it has no name.</summary>
    public string Header { get; }

    /// <summary>Populated only for a rejected pack's header section - null for an ordinary pack's.</summary>
    public RejectedPack? RejectedPack { get; }

    public bool IsRejectedPack => RejectedPack is not null;

    public IReadOnlyList<RegisteredEntry> ValidRows { get; }

    public IReadOnlyList<ContentBrokenRow> BrokenRows { get; }

    /// <summary>The count a section's own header shows: every row currently visible in it.</summary>
    public int ShownCount => ValidRows.Count + BrokenRows.Count;

    public static ContentSection ForPack(
        string packName, IReadOnlyList<RegisteredEntry> validRows, IReadOnlyList<ContentBrokenRow> brokenRows) =>
        new(packName, rejectedPack: null, validRows, brokenRows);

    public static ContentSection ForRejectedPack(RejectedPack pack) => new(pack.Location, pack, [], []);
}
