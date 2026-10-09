using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.State;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Desktop.Entries;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// State for the "Świat kampanii" desk tool: every entity this campaign holds, and a picker to
/// bring a new one in from this system's own resolved entries. The first consumer of the
/// <c>entries.instances</c> state model and <see cref="EntityResolver"/> anywhere in
/// the application.
/// <para>
/// Kept in its own file, free of any Avalonia control reference, so it can be exercised without a
/// window.
/// </para>
/// </summary>
public sealed partial class CampaignEntitiesToolViewModel : ObservableObject, IDisposable
{
    private readonly CampaignEntriesContext _context;
    private readonly ContentId _ownerSet;

    private bool _isDisposed;

    public CampaignEntitiesToolViewModel(CampaignEntriesContext context, ContentId ownerSet)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _ownerSet = ownerSet;

        // Every entry this system owns and that resolved cleanly - never one belonging to
        // another system, and never one the registry already marked broken. The registry itself does
        // not change while a campaign is open, so this list is built once rather than on every
        // refresh.
        AddableEntries =
        [
            .. context.Registry.Entries
                .Where(entry => entry.Unresolved is null && entry.Entry.Type.Set == ownerSet)
                .OrderBy(entry => entry.Entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(entry => new AddableEntryOption(entry.Entry.Name, entry.Address)),
        ];

        _context.Changed += OnChanged;

        Refresh();
    }

    /// <summary>Every entity this campaign holds, resolved or not - an unresolved one never disappears.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<EntityRowViewModel> Entities { get; private set; } = [];

    public bool HasEntities => Entities.Count > 0;

    /// <summary>Entries this system can offer for adding - see the constructor for the filter.</summary>
    public IReadOnlyList<AddableEntryOption> AddableEntries { get; }

    /// <summary>
    /// The picker's selection. A plain store: picking an entry arms <see cref="AddCommand"/> and
    /// nothing else. Writing from the setter instead would mean starting a save nobody can await -
    /// its failure would surface nowhere, and a test could only assert the result by relying on the
    /// repository happening to finish synchronously.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial AddableEntryOption? SelectedToAdd { get; set; }

    /// <summary>The sentence for the GM after the last write attempt - null once it went through cleanly.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    public partial string? Message { get; private set; }

    public bool HasMessage => Message is not null;

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _context.Changed -= OnChanged;

        AddCommand.NotifyCanExecuteChanged();
    }

    private void OnChanged(CampaignStateSnapshot snapshot) => Refresh();

    /// <summary>Brings <see cref="SelectedToAdd"/> into the campaign as a new entity, and saves it.</summary>
    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        if (SelectedToAdd is not { } option)
        {
            return;
        }

        var result = await _context.ChangeAsync(CampaignEntityChanges.Add(_context.Snapshot, option.Address, label: null));
        Message = result.Message;

        // Cleared whether or not the write went through: the entity either exists now, or the
        // message above says why it does not, and in both cases leaving the entry selected invites
        // adding it a second time by accident.
        SelectedToAdd = null;
    }

    private bool CanAdd() => SelectedToAdd is not null && !_isDisposed;

    private void Refresh()
    {
        if (_isDisposed)
        {
            return;
        }

        Entities =
        [
            .. _context.Snapshot.Get(EntitiesModel.Declaration).Values
                .Select(BuildRow)
                .OrderBy(row => row.DisplayName, StringComparer.CurrentCultureIgnoreCase),
        ];

        OnPropertyChanged(nameof(HasEntities));
    }

    private EntityRowViewModel BuildRow(CampaignEntity entity)
    {
        var resolved = _context.Resolver.Resolve(entity);
        var name = entity.Label ?? resolved.Source?.Entry.Name ?? entity.Source.ToString();
        var message = resolved.Unresolved is { } reason ? Describe(reason, entity, resolved) : null;

        return new EntityRowViewModel(_context, name, message, resolved, _ownerSet);
    }

    /// <summary>
    /// One Polish sentence per <see cref="EntityUnresolvedReason"/> - never a shared generic
    /// fallback, mirroring how a content tab explains a broken entry. The values-rejected case
    /// appends the system's own explanation (<see cref="ResolvedEntity.UnresolvedDetail"/>),
    /// because that text is the only place the reason for the rejection lives.
    /// </summary>
    private static string Describe(EntityUnresolvedReason reason, CampaignEntity entity, ResolvedEntity resolved) =>
        reason switch
        {
            EntityUnresolvedReason.MissingPack =>
                $"Paczka „{entity.Source.Pack}” nie jest zainstalowana.",
            EntityUnresolvedReason.MissingEntry =>
                $"Paczka „{entity.Source.Pack}” nie zawiera już wpisu „{entity.Source.Entry}”.",
            EntityUnresolvedReason.EntryUnresolved =>
                "Wpis, na który wskazuje ta instancja, nie związał się z żadnym typem treści.",
            EntityUnresolvedReason.ValuesRejected =>
                $"System odrzucił wartości tej instancji: {resolved.UnresolvedDetail}",
            _ => "Tej instancji nie da się wyświetlić.",
        };
}
