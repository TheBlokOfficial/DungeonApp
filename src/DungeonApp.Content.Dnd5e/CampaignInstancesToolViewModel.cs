using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// State for the "Świat kampanii" desk tool: every instance this campaign holds, and a picker to
/// bring a new one in from this system's own resolved entries. The first consumer of
/// <see cref="Core.Content.CampaignInstances"/> and <see cref="Core.Content.InstanceResolver"/> -
/// nothing in the engine or the shell reads either of them yet.
/// <para>
/// Kept in its own file, free of any Avalonia control reference, so it can be exercised without a
/// window.
/// </para>
/// </summary>
public sealed class CampaignInstancesToolViewModel : ObservableObject, IDisposable
{
    private readonly CampaignToolContext _context;
    private readonly ContentId _ownerSet;
    private readonly IDisposable[] _subscriptions;

    private IReadOnlyList<InstanceRowViewModel> _instances = [];
    private AddableEntryOption? _selectedToAdd;
    private string? _message;
    private bool _isDisposed;

    public CampaignInstancesToolViewModel(CampaignToolContext context, ContentId ownerSet)
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

        AddCommand = new AsyncCommand(AddSelectedAsync, () => _selectedToAdd is not null && !_isDisposed);

        _subscriptions =
        [
            context.Events.Subscribe<InstanceAdded>(_ => Refresh()),
            context.Events.Subscribe<InstanceRemoved>(_ => Refresh()),
            context.Events.Subscribe<InstanceRelabelled>(_ => Refresh()),
            context.Events.Subscribe<InstancePatchReplaced>(_ => Refresh()),
        ];

        Refresh();
    }

    /// <summary>Every instance this campaign holds, resolved or not - an unresolved one never disappears.</summary>
    public IReadOnlyList<InstanceRowViewModel> Instances
    {
        get => _instances;
        private set => SetField(ref _instances, value);
    }

    public bool HasInstances => Instances.Count > 0;

    /// <summary>Entries this system can offer for adding - see the constructor for the filter.</summary>
    public IReadOnlyList<AddableEntryOption> AddableEntries { get; }

    /// <summary>
    /// The picker's selection. A plain store: picking an entry arms <see cref="AddCommand"/> and
    /// nothing else. Writing from the setter instead would mean starting a save nobody can await -
    /// its failure would surface nowhere, and a test could only assert the result by relying on the
    /// repository happening to finish synchronously.
    /// </summary>
    public AddableEntryOption? SelectedToAdd
    {
        get => _selectedToAdd;
        set
        {
            if (SetField(ref _selectedToAdd, value))
            {
                AddCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Brings <see cref="SelectedToAdd"/> into the campaign as a new instance, and saves it.</summary>
    public AsyncCommand AddCommand { get; }

    /// <summary>The sentence for the GM after the last write attempt - null once it went through cleanly.</summary>
    public string? Message
    {
        get => _message;
        private set
        {
            if (SetField(ref _message, value))
            {
                RaisePropertyChanged(nameof(HasMessage));
            }
        }
    }

    public bool HasMessage => Message is not null;

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        AddCommand.RaiseCanExecuteChanged();
    }

    private async Task AddSelectedAsync()
    {
        if (_selectedToAdd is not { } option)
        {
            return;
        }

        Message = await _context.ExecuteAsync(() => _context.Instances.Add(option.Address, label: null));

        // Cleared whether or not the write went through: the instance either exists now, or the
        // message above says why it does not, and in both cases leaving the entry selected invites
        // adding it a second time by accident.
        SelectedToAdd = null;
    }

    private void Refresh()
    {
        if (_isDisposed)
        {
            return;
        }

        Instances =
        [
            .. _context.Instances.All
                .Select(BuildRow)
                .OrderBy(row => row.DisplayName, StringComparer.CurrentCultureIgnoreCase),
        ];

        RaisePropertyChanged(nameof(HasInstances));
    }

    private InstanceRowViewModel BuildRow(CampaignInstance instance)
    {
        var resolved = _context.Resolver.Resolve(instance);
        var name = instance.Label ?? resolved.Source?.Entry.Name ?? instance.Source.ToString();
        var message = resolved.Unresolved is { } reason ? Describe(reason, instance, resolved) : null;

        return new InstanceRowViewModel(_context, instance.Id, name, message, resolved, _ownerSet);
    }

    /// <summary>
    /// One Polish sentence per <see cref="InstanceUnresolvedReason"/> - never a shared generic
    /// fallback, mirroring how the registry screen explains a broken entry. The values-rejected case
    /// appends the system's own explanation (<see cref="ResolvedInstance.UnresolvedDetail"/>),
    /// because that text is the only place the reason for the rejection lives.
    /// </summary>
    private static string Describe(InstanceUnresolvedReason reason, CampaignInstance instance, ResolvedInstance resolved) =>
        reason switch
        {
            InstanceUnresolvedReason.MissingPack =>
                $"Paczka „{instance.Source.Pack}” nie jest zainstalowana.",
            InstanceUnresolvedReason.MissingEntry =>
                $"Paczka „{instance.Source.Pack}” nie zawiera już wpisu „{instance.Source.Entry}”.",
            InstanceUnresolvedReason.EntryUnresolved =>
                "Wpis, na który wskazuje ta instancja, nie związał się z żadnym typem treści.",
            InstanceUnresolvedReason.ValuesRejected =>
                $"System odrzucił wartości tej instancji: {resolved.UnresolvedDetail}",
            _ => "Tej instancji nie da się wyświetlić.",
        };
}
