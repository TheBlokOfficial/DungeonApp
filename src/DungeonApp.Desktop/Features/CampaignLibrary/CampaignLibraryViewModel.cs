using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Persistence;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignLibrary;

/// <summary>
/// The shelf the GM lands on before any campaign is open. A backstage screen in the vocabulary of
/// the visual direction: a flat surface with stable forms, not the recessed desk.
/// <para>
/// It owns presentation state only. Naming rules live in <see cref="CampaignName"/> and creation
/// lives in <see cref="CreateCampaign"/>; this class decides what the GM is told, not what is legal.
/// </para>
/// <para>
/// <see cref="SetActiveSystem"/> is called once per system choice, before the shelf is shown for real
/// (<c>AppShellViewModel.ChooseSystemAsync</c>). Before it is ever called - during the startup
/// warmup pass, which runs before any system is chosen - <see cref="LoadAsync"/> shows every campaign
/// unfiltered, since nothing about that pass is ever seen by the GM. Once an active system is set,
/// the shelf filters to docs/architecture.md's "Pasek boczny: trzy kategorie" rule: the active
/// system's own campaigns (available or not), plus any campaign that cannot be assigned to *any*
/// compiled system at all; a campaign belonging to a different, compiled system is hidden rather than
/// shown unavailable.
/// </para>
/// </summary>
public sealed class CampaignLibraryViewModel : ObservableObject
{
    private readonly ICampaignRepository _campaigns;
    private readonly CreateCampaign _createCampaign;
    private readonly CampaignPreparationCache _preparations;
    private readonly IReadOnlyList<IGameSystem> _systems;
    private readonly Func<CampaignSummary, Task> _openCampaign;

    private IGameSystem? _activeSystem;
    private string _newCampaignName = string.Empty;
    private string? _nameError;
    private bool _isBusy;
    private bool _isLoaded;

    public CampaignLibraryViewModel(
        ICampaignRepository campaigns,
        CreateCampaign createCampaign,
        CampaignPreparationCache preparations,
        IReadOnlyList<IGameSystem> systems,
        Func<CampaignSummary, Task> openCampaign)
    {
        _campaigns = campaigns;
        _createCampaign = createCampaign;
        _preparations = preparations;
        _systems = systems;
        _openCampaign = openCampaign;

        CreateCommand = new AsyncCommand(CreateAsync, () => CanCreate);
    }

    public ObservableCollection<CampaignRowViewModel> Campaigns { get; } = [];

    public AsyncCommand CreateCommand { get; }

    /// <summary>
    /// Bound to the name box. Every keystroke re-asks the Core for a verdict rather than repeating
    /// the rules here, which is what keeps the two from drifting apart.
    /// </summary>
    public string NewCampaignName
    {
        get => _newCampaignName;
        set
        {
            if (!SetField(ref _newCampaignName, value))
            {
                return;
            }

            // An untouched, empty box is not a mistake yet - do not scold the GM before they type.
            NameError = value.Length == 0 ? null : DescribeNameError(CampaignName.Validate(value));

            RaisePropertyChanged(nameof(CanCreate));
            CreateCommand.RaiseCanExecuteChanged();
        }
    }

    public string? NameError
    {
        get => _nameError;
        private set
        {
            if (SetField(ref _nameError, value))
            {
                RaisePropertyChanged(nameof(HasNameError));
            }
        }
    }

    public bool HasNameError => !string.IsNullOrWhiteSpace(NameError);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                RaisePropertyChanged(nameof(CanCreate));
                CreateCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanCreate => !IsBusy && _activeSystem is not null && CampaignName.Validate(NewCampaignName) is CampaignNameError.None;

    /// <summary>
    /// True only once a read has actually finished. Without the loaded flag the empty state would
    /// flash on every startup before the first campaign arrives.
    /// </summary>
    public bool IsEmpty => _isLoaded && Campaigns.Count == 0;

    public bool HasCampaigns => Campaigns.Count > 0;

    /// <summary>
    /// Called once per system choice, before the shelf is shown for real - see the type's own remarks.
    /// Does not reload by itself; the caller is expected to call <see cref="LoadAsync"/> right after.
    /// </summary>
    public void SetActiveSystem(IGameSystem system)
    {
        _activeSystem = system;
        RaisePropertyChanged(nameof(CanCreate));
        CreateCommand.RaiseCanExecuteChanged();
    }

    public async Task<IReadOnlyList<CampaignSummary>> LoadAsync()
    {
        IsBusy = true;
        IReadOnlyList<CampaignSummary> summaries = [];

        try
        {
            summaries = await _campaigns.ListAsync();

            var activeSystem = _activeSystem;
            var visible = activeSystem is null ? summaries : summaries.Where(summary => ShouldShow(summary, activeSystem)).ToArray();

            Campaigns.Clear();

            foreach (var summary in visible)
            {
                // During the pre-system warmup pass nothing here is ever shown to the GM, so there is
                // no point paying for a full read per campaign - every row simply reports itself
                // available until the real, per-system reload replaces it.
                var availability = activeSystem is null
                    ? CampaignAvailability.Available
                    : await _preparations.CheckAvailabilityAsync(summary);

                Campaigns.Add(new CampaignRowViewModel(summary, availability, OpenAsync, DeleteAsync));
            }

            _isLoaded = true;
            RaisePropertyChanged(nameof(IsEmpty));
            RaisePropertyChanged(nameof(HasCampaigns));
        }
        // The shelf is also part of startup preparation. A storage failure is presentation state,
        // not a reason to abort the readiness pipeline and leave the shell permanently gated.
        catch (Exception ex) when (ex is CampaignStoreException or System.IO.IOException or UnauthorizedAccessException)
        {
            // Persistent feedback belongs to a future error state, not a temporary toast.
        }
        finally
        {
            IsBusy = false;
        }

        return summaries;
    }

    /// <summary>
    /// A campaign is shown on <paramref name="activeSystem"/>'s shelf when it is that system's own, or
    /// when it cannot be assigned to any compiled system at all (no system recorded, or one this build
    /// does not know) - docs/architecture.md, "Kampania należy do jednego systemu". A campaign belonging
    /// to a different, compiled system is hidden rather than shown unavailable.
    /// </summary>
    private bool ShouldShow(CampaignSummary summary, IGameSystem activeSystem)
    {
        if (summary.SystemId is not { } systemId)
        {
            return true;
        }

        if (systemId == activeSystem.Id)
        {
            return true;
        }

        return !_systems.Any(system => system.Id == systemId);
    }

    private async Task CreateAsync()
    {
        if (_activeSystem is not { } system)
        {
            return;
        }

        IsBusy = true;

        try
        {
            await _createCampaign.ExecuteAsync(NewCampaignName, system.Id, system.StateModels);

            NewCampaignName = string.Empty;
        }
        catch (System.IO.IOException)
        {
            // Persistent feedback belongs to a future error state, not a temporary toast.
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync();
    }

    private async Task OpenAsync(CampaignRowViewModel row)
    {
        IsBusy = true;

        try
        {
            await _openCampaign(row.Summary);
        }
        catch (CampaignStoreException)
        {
        }
        catch (System.IO.IOException)
        {
        }
        catch (CampaignUnavailableException)
        {
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteAsync(CampaignRowViewModel row)
    {
        IsBusy = true;
        try
        {
            await _campaigns.DeleteAsync(row.Id);
            Campaigns.Remove(row);
            RaisePropertyChanged(nameof(IsEmpty));
            RaisePropertyChanged(nameof(HasCampaigns));
        }
        catch (System.IO.IOException)
        {
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string? DescribeNameError(CampaignNameError error) => error switch
    {
        CampaignNameError.Empty => "Nazwa nie może być pusta.",
        CampaignNameError.TooLong => $"Nazwa może mieć najwyżej {CampaignName.MaxLength} znaków.",
        _ => null
    };

}
