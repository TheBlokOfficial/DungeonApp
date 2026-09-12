using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.Registry;

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

        // Deterministic regardless of the loader's own directory-scan order: name first, then the
        // full address, so two entries sharing a name still land in a stable order.
        Entries =
        [
            .. registry.Entries
                .Select(entry => new RegistryEntryRowViewModel(
                    entry,
                    packNamesById.TryGetValue(entry.Address.Pack, out var packName)
                        ? packName
                        : entry.Address.Pack.ToString()))
                .OrderBy(row => row.Name, StringComparer.Ordinal)
                .ThenBy(row => row.Address, StringComparer.Ordinal)
        ];
    }

    public IReadOnlyList<RegistryEntryRowViewModel> Entries { get; }

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

    /// <summary>A Polish, reason-specific explanation for the selected entry, or null when it resolved.</summary>
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
        var selected = SelectedEntry?.RegisteredEntry;

        if (selected is null)
        {
            Card = null;
            UnresolvedMessage = null;
            return;
        }

        if (selected.Unresolved is { } reason)
        {
            Card = null;
            UnresolvedMessage = DescribeUnresolved(reason, selected);
            return;
        }

        UnresolvedMessage = null;
        Card = _presentation.CreateCard(selected.Entry);
    }

    private static string DescribeUnresolved(EntryUnresolvedReason reason, RegisteredEntry entry)
    {
        var setId = entry.Entry.Type.Set.ToString();
        var typeId = entry.Entry.Type.Type.ToString();

        return reason switch
        {
            EntryUnresolvedReason.MissingSet =>
                $"Ten wpis odwołuje się do zestawu treści „{setId}”, który nie jest zainstalowany.",
            EntryUnresolvedReason.MissingType =>
                $"Zestaw „{setId}” nie zna już typu treści „{typeId}”.",
            EntryUnresolvedReason.TypeVersionMismatch =>
                $"Typ treści „{typeId}” w zestawie „{setId}” zmienił się od czasu zapisania tego wpisu. Trzeba go zapisać ponownie.",
            EntryUnresolvedReason.ValuesRejected =>
                $"Zestaw „{setId}” nie przyjął wartości tego wpisu dla typu „{typeId}”: {entry.UnresolvedDetail}",
            _ => "Tego wpisu nie da się wyświetlić."
        };
    }
}
