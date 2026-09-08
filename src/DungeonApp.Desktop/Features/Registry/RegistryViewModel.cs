using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.Registry;

/// <summary>
/// View model for the registry screen: every entry every installed content pack declares, and the
/// composed card for whichever one is selected.
/// <para>
/// Built once, lazily, on first entry into the registry section (see
/// <see cref="Shell.AppShellViewModel"/>) and held for the lifetime of the shell - the same shape as
/// the campaign workspace, not something rebuilt on every visit.
/// </para>
/// </summary>
public sealed class RegistryViewModel : ObservableObject
{
    private RegistryEntryRowViewModel? _selectedEntry;
    private IReadOnlyList<object> _card = [];
    private string? _unresolvedMessage;

    public RegistryViewModel(ContentRegistry registry)
    {
        var packNamesById = registry.ContentPacks.ToDictionary(pack => pack.Id, pack => pack.Name);

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
    /// The selected entry's card, in template order - empty when nothing is selected, when the
    /// selected entry is unresolved, or (rarely) when a resolved entry supplied no value any element
    /// could render.
    /// </summary>
    public IReadOnlyList<object> Card
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

    public bool HasCard => Card.Count > 0;

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
            Card = [];
            UnresolvedMessage = null;
            return;
        }

        if (selected.Unresolved is { } reason)
        {
            Card = [];
            UnresolvedMessage = DescribeUnresolved(reason, selected);
            return;
        }

        UnresolvedMessage = null;
        Card = CardViewModel.Build(selected.Template!, selected);
    }

    private static string DescribeUnresolved(EntryUnresolvedReason reason, RegisteredEntry entry)
    {
        var packId = entry.Entry.Template.Pack.ToString();
        var templateId = entry.Entry.Template.Template.ToString();

        return reason switch
        {
            EntryUnresolvedReason.MissingPack =>
                $"Ten wpis odwołuje się do paczki „{packId}”, która nie jest zainstalowana.",
            EntryUnresolvedReason.MissingTemplate =>
                $"Paczka „{packId}” nie zawiera już szablonu „{templateId}”.",
            EntryUnresolvedReason.TemplateVersionMismatch =>
                $"Szablon „{templateId}” w paczce „{packId}” zmienił się od czasu zapisania tego wpisu. Trzeba go zapisać ponownie.",
            _ => "Tego wpisu nie da się wyświetlić."
        };
    }
}
