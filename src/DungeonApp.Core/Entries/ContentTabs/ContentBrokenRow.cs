namespace DungeonApp.Core.Entries;

/// <summary>
/// One broken row in a content tab: either a <see cref="RegisteredEntry"/> that parsed but never
/// bound to a content type, or a <see cref="RejectedEntry"/> file that never became an entry at all
/// - never both, never neither, the same discipline <see cref="RegisteredEntry"/> itself follows.
/// <see cref="DisplayName"/> is the entry's own name when there is one, and the file's location when
/// there is not (docs/architecture.md, "Zakładki treści": broken rows sort "po nazwie, a gdy nazwy
/// nie ma — po nazwie pliku").
/// </summary>
public sealed record ContentBrokenRow
{
    private ContentBrokenRow(RegisteredEntry? unresolvedEntry, RejectedEntry? rejectedFile, string displayName)
    {
        UnresolvedEntry = unresolvedEntry;
        RejectedFile = rejectedFile;
        DisplayName = displayName;
    }

    public RegisteredEntry? UnresolvedEntry { get; }

    public RejectedEntry? RejectedFile { get; }

    public string DisplayName { get; }

    /// <summary>The pack this row lives in, read off whichever half is populated.</summary>
    public ContentId Pack => UnresolvedEntry is { } entry ? entry.Address.Pack : RejectedFile!.Pack;

    public static ContentBrokenRow ForUnresolvedEntry(RegisteredEntry entry) =>
        new(entry, rejectedFile: null, entry.Entry.Name);

    public static ContentBrokenRow ForRejectedFile(RejectedEntry file) =>
        new(unresolvedEntry: null, file, file.Location);
}
