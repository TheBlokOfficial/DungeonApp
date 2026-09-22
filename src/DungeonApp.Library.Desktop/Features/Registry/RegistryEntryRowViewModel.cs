using System;
using DungeonApp.Core.Content;

namespace DungeonApp.Library.Desktop.Features.Registry;

/// <summary>
/// One row of the registry's entry list. Wraps either a <see cref="Content.RegisteredEntry"/> that
/// parsed fine (resolved or unresolved) or a <see cref="Content.RejectedEntry"/> that never became
/// one - never both, never neither. The two factory methods are the only way to build one, which is
/// what makes that invariant unbreakable rather than merely documented, mirroring
/// <see cref="Content.RegisteredEntry"/>'s own discipline.
/// </summary>
public sealed class RegistryEntryRowViewModel
{
    private readonly RegisteredEntry? _registeredEntry;
    private readonly RejectedEntry? _rejectedEntry;

    private RegistryEntryRowViewModel(
        RegisteredEntry? registeredEntry, RejectedEntry? rejectedEntry, string packName, bool showsNotLoadedHeader)
    {
        _registeredEntry = registeredEntry;
        _rejectedEntry = rejectedEntry;
        PackName = packName;
        ShowsNotLoadedHeader = showsNotLoadedHeader;
    }

    /// <summary>
    /// The wrapped entry, once <see cref="IsNotLoaded"/> is false - null for a row built from a file
    /// that never became an entry.
    /// </summary>
    public RegisteredEntry? RegisteredEntry => _registeredEntry;

    public string Name => _registeredEntry is { } entry ? entry.Entry.Name : _rejectedEntry!.Location;

    /// <summary>
    /// Where this row lives. For an entry row, the full <c>pack:entry</c> address, as always. For a
    /// not-loaded row there is no entry id to build an <see cref="EntryAddress"/> from - the file
    /// never parsed far enough to have one - so this answers the same question ("where does this
    /// live") with the only part that exists: the pack id alone. It is deliberately not an
    /// address-shaped string built from the file path; that would put something that is not an
    /// address in a slot that means address.
    /// </summary>
    public string Address => _registeredEntry is { } entry ? entry.Address.ToString() : _rejectedEntry!.Pack.ToString();

    public string PackName { get; }

    /// <summary>Empty for an unresolved entry or a not-loaded row - there is no content type to name.</summary>
    public string TypeName => _registeredEntry?.Type?.Name ?? string.Empty;

    public bool IsUnresolved => _registeredEntry?.Unresolved is not null;

    /// <summary>True for a row built from a file that never became an entry at all.</summary>
    public bool IsNotLoaded => _rejectedEntry is not null;

    /// <summary>
    /// Whether the row's third line should show <see cref="TypeName"/> - false for an unresolved
    /// entry (which shows "Nierozwiązany wpis" there instead) and false for a not-loaded row (which
    /// shows neither; the "NIE WCZYTANE" header above it is what marks that row).
    /// </summary>
    public bool ShowsTypeName => !IsUnresolved && !IsNotLoaded;

    /// <summary>
    /// The loader's own diagnostic text for why this file was rejected - populated only alongside
    /// <see cref="IsNotLoaded"/>.
    /// </summary>
    public string? NotLoadedReason => _rejectedEntry?.Reason;

    /// <summary>
    /// True on exactly one row: the first not-loaded row in the list, where the "NIE WCZYTANE"
    /// header belongs. False on every other row, and false on every row when nothing is broken.
    /// <para>
    /// The header rides on a row rather than standing between two lists, so the screen stays one
    /// list with one item template - no second items control, no grouping, and nothing that has to
    /// be kept in step with the ordering the view model already fixed. The whole price is that this
    /// one row is taller than its neighbours, and that is the trade being taken deliberately.
    /// </para>
    /// </summary>
    public bool ShowsNotLoadedHeader { get; }

    public static RegistryEntryRowViewModel ForEntry(RegisteredEntry entry, string packName)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new RegistryEntryRowViewModel(entry, rejectedEntry: null, packName, showsNotLoadedHeader: false);
    }

    public static RegistryEntryRowViewModel ForNotLoadedFile(
        RejectedEntry rejected, string packName, bool showsNotLoadedHeader)
    {
        ArgumentNullException.ThrowIfNull(rejected);

        return new RegistryEntryRowViewModel(registeredEntry: null, rejected, packName, showsNotLoadedHeader);
    }
}
