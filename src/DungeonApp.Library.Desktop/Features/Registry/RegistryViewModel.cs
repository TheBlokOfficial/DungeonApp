using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Library.Desktop.Features.Registry;

/// <summary>
/// View model for the registry screen: every entry every installed pack declares, and the finished
/// card for whichever one is selected.
/// <para>
/// Built once, lazily, on first entry into the registry section (see
/// <see cref="Shell.AppShellViewModel"/>) and held for the lifetime of the shell - the same shape as
/// the campaign workspace, not something rebuilt on every visit.
/// </para>
/// </summary>
public sealed class RegistryViewModel : ObservableObject
{
    private readonly IContentPresentation _presentation;

    private RegistryEntryRowViewModel? _selectedEntry;
    private Control? _card;
    private string? _unresolvedMessage;

    public RegistryViewModel(ContentRegistry registry, IContentPresentation presentation)
    {
        _presentation = presentation;

        var packNamesById = registry.Packs.ToDictionary(pack => pack.Id, pack => pack.Name);

        string PackNameFor(ContentId packId) =>
            packNamesById.TryGetValue(packId, out var packName) ? packName : packId.ToString();

        // Deterministic regardless of the loader's own directory-scan order: name first, then the
        // full address, so two entries sharing a name still land in a stable order.
        var entryRows = registry.Entries
            .Select(entry => RegistryEntryRowViewModel.ForEntry(entry, PackNameFor(entry.Address.Pack)))
            .OrderBy(row => row.Name, StringComparer.Ordinal)
            .ThenBy(row => row.Address, StringComparer.Ordinal);

        // Same idea, one level down: files that never became an entry at all, after every entry,
        // ordered by pack id then file location so the group is just as deterministic.
        var notLoadedRows = registry.RejectedEntries
            .OrderBy(rejected => rejected.Pack.ToString(), StringComparer.Ordinal)
            .ThenBy(rejected => rejected.Location, StringComparer.Ordinal)
            .Select((rejected, index) => RegistryEntryRowViewModel.ForNotLoadedFile(
                rejected, PackNameFor(rejected.Pack), showsNotLoadedHeader: index == 0));

        Entries = [.. entryRows, .. notLoadedRows];
    }

    public IReadOnlyList<RegistryEntryRowViewModel> Entries { get; }

    /// <summary>
    /// Counts the not-loaded rows too, so a registry holding nothing but broken files is
    /// <em>not</em> empty - it has rows to show and a reason to give for each of them. Only a
    /// registry with no entries and no rejected files at all is empty, which is the one case
    /// <see cref="ShowSelectionPrompt"/> falls silent for.
    /// </summary>
    public bool IsEmpty => Entries.Count == 0;

    public RegistryEntryRowViewModel? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetField(ref _selectedEntry, value))
            {
                RaisePropertyChanged(nameof(HasSelection));
                RaisePropertyChanged(nameof(ShowSelectionPrompt));
                RecomputeCard();
            }
        }
    }

    public bool HasSelection => SelectedEntry is not null;

    /// <summary>
    /// Whether to invite the reader to pick something. An empty registry already says why the list
    /// is bare, and telling someone to choose from a list that has nothing in it contradicts that
    /// on the same screen. <see cref="IsEmpty"/> never changes for a given instance, so this only
    /// has to be raised alongside the selection.
    /// </summary>
    public bool ShowSelectionPrompt => !HasSelection && !IsEmpty;

    /// <summary>
    /// The selected entry's finished card - null when nothing is selected or the selected entry is
    /// unresolved. Built through <see cref="IContentPresentation.CreateCard"/>, which is the only
    /// place that ever knows what the card looks like; this view model never inspects it.
    /// </summary>
    public Control? Card
    {
        get => _card;
        private set
        {
            if (SetField(ref _card, value))
            {
                RaisePropertyChanged(nameof(HasCard));
            }
        }
    }

    public bool HasCard => Card is not null;

    /// <summary>
    /// A Polish, reason-specific explanation for the selected row - either why an entry is
    /// unresolved or why a file never became an entry at all - or null when the selected entry
    /// resolved cleanly.
    /// </summary>
    public string? UnresolvedMessage
    {
        get => _unresolvedMessage;
        private set
        {
            if (SetField(ref _unresolvedMessage, value))
            {
                RaisePropertyChanged(nameof(HasUnresolvedMessage));
            }
        }
    }

    public bool HasUnresolvedMessage => UnresolvedMessage is not null;

    private void RecomputeCard()
    {
        var row = SelectedEntry;

        if (row is null)
        {
            Card = null;
            UnresolvedMessage = null;
            return;
        }

        if (row.IsNotLoaded)
        {
            Card = null;
            UnresolvedMessage = DescribeNotLoaded(row.NotLoadedReason!);
            return;
        }

        var selected = row.RegisteredEntry!;

        if (selected.Unresolved is { } reason)
        {
            Card = null;
            UnresolvedMessage = DescribeUnresolved(reason, selected);
            return;
        }

        UnresolvedMessage = null;
        Card = _presentation.CreateCard(selected.Entry);
    }

    /// <summary>
    /// Frames the loader's own English diagnostic text in one Polish sentence, the same way
    /// <see cref="DescribeUnresolved"/> frames a system's rejection detail - never translating
    /// or rewording the diagnostic itself.
    /// </summary>
    private static string DescribeNotLoaded(string reason) => $"Nie udało się wczytać tego pliku: {reason}";

    private static string DescribeUnresolved(EntryUnresolvedReason reason, RegisteredEntry entry)
    {
        var setId = entry.Entry.Type.Set.ToString();
        var typeId = entry.Entry.Type.Type.ToString();

        return reason switch
        {
            EntryUnresolvedReason.MissingSet =>
                $"Ten wpis odwołuje się do systemu „{setId}”, który nie jest zainstalowany.",
            EntryUnresolvedReason.MissingType =>
                $"System „{setId}” nie zna już typu treści „{typeId}”.",
            EntryUnresolvedReason.TypeVersionMismatch =>
                $"Typ treści „{typeId}” w systemie „{setId}” zmienił się od czasu zapisania tego wpisu. Trzeba go zapisać ponownie.",
            EntryUnresolvedReason.ValuesRejected =>
                $"System „{setId}” nie przyjął wartości tego wpisu dla typu „{typeId}”: {entry.UnresolvedDetail}",
            _ => "Tego wpisu nie da się wyświetlić."
        };
    }
}
