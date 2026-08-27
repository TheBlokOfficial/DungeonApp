using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Modules;
using DungeonApp.Core.Persistence;
using DungeonApp.Desktop.Features.CampaignWorkspace;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignLibrary;

/// <summary>
/// The shelf the GM lands on before any campaign is open. A backstage screen in the vocabulary of
/// the visual direction: a flat surface with stable forms, not the recessed desk.
/// <para>
/// It owns presentation state only. Naming rules live in <see cref="CampaignName"/> and creation
/// lives in <see cref="CreateCampaign"/>; this class decides what the GM is told, not what is legal.
/// </para>
/// </summary>
public sealed class CampaignLibraryViewModel : ObservableObject
{
    private readonly ICampaignRepository _campaigns;
    private readonly CreateCampaign _createCampaign;
    private readonly ModuleCatalog _modules;
    private readonly Func<CampaignId, Task> _openCampaign;

    private string _newCampaignName = string.Empty;
    private string? _nameError;
    private string? _status;
    private bool _isBusy;
    private bool _isLoaded;

    /// <summary>Guards the pass that resolves dependencies, which ticks boxes of its own.</summary>
    private bool _isResolving;

    public CampaignLibraryViewModel(
        ICampaignRepository campaigns,
        CreateCampaign createCampaign,
        ModuleCatalog modules,
        Func<CampaignId, Task> openCampaign)
    {
        _campaigns = campaigns;
        _createCampaign = createCampaign;
        _modules = modules;
        _openCampaign = openCampaign;

        // Everything this build offers, all switched on. A campaign is cheap to start and awkward
        // to extend later, so the default is the full desk rather than a bare one.
        foreach (var manifest in modules.Manifests)
        {
            ModuleChoices.Add(new ModuleChoiceViewModel(manifest, isChosen: true, ResolveModuleChoices));
        }

        ResolveModuleChoices();

        CreateCommand = new AsyncCommand(CreateAsync, () => CanCreate);
    }

    public ObservableCollection<CampaignRowViewModel> Campaigns { get; } = [];

    /// <summary>What the new campaign will be able to do. Fixed once it is created.</summary>
    public ObservableCollection<ModuleChoiceViewModel> ModuleChoices { get; } = [];

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
        private set => SetField(ref _nameError, value);
    }

    /// <summary>The last thing that happened, shown under the list. Null when there is nothing to say.</summary>
    public string? Status
    {
        get => _status;
        private set
        {
            if (SetField(ref _status, value))
            {
                RaisePropertyChanged(nameof(HasStatus));
            }
        }
    }

    public bool HasStatus => !string.IsNullOrWhiteSpace(Status);

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

    public bool CanCreate => !IsBusy && CampaignName.Validate(NewCampaignName) is CampaignNameError.None;

    /// <summary>
    /// True only once a read has actually finished. Without the loaded flag the empty state would
    /// flash on every startup before the first campaign arrives.
    /// </summary>
    public bool IsEmpty => _isLoaded && Campaigns.Count == 0;

    public bool HasCampaigns => Campaigns.Count > 0;

    public async Task<IReadOnlyList<CampaignSummary>> LoadAsync()
    {
        IsBusy = true;
        IReadOnlyList<CampaignSummary> summaries = [];

        try
        {
            summaries = await _campaigns.ListAsync();

            Campaigns.Clear();

            foreach (var summary in summaries)
            {
                Campaigns.Add(new CampaignRowViewModel(summary, OpenAsync));
            }

            _isLoaded = true;
            RaisePropertyChanged(nameof(IsEmpty));
            RaisePropertyChanged(nameof(HasCampaigns));
        }
        // The shelf is also part of startup preparation. A storage failure is presentation state,
        // not a reason to abort the readiness pipeline and leave the shell permanently gated.
        catch (Exception ex) when (ex is CampaignStoreException or System.IO.IOException or UnauthorizedAccessException)
        {
            Status = "Nie udało się odczytać biblioteki kampanii.";
        }
        finally
        {
            IsBusy = false;
        }

        return summaries;
    }

    private async Task CreateAsync()
    {
        IsBusy = true;

        try
        {
            var campaign = await _createCampaign.ExecuteAsync(NewCampaignName, ChosenModules());

            NewCampaignName = string.Empty;
            Status = $"Utworzono kampanię „{campaign.Name.Value}”.";
        }
        catch (System.IO.IOException)
        {
            Status = "Nie udało się zapisać kampanii na dysku.";
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync();
    }

    private IReadOnlyList<ModuleId> ChosenModules() =>
        [.. ModuleChoices.Where(choice => choice.IsChosen).Select(choice => choice.Id)];

    /// <summary>
    /// Ticks and locks whatever the current choice needs. The Core owns what "needs" means; this
    /// only has to say which module is holding which, so a locked box is explained.
    /// </summary>
    private void ResolveModuleChoices()
    {
        if (_isResolving)
        {
            return;
        }

        _isResolving = true;

        try
        {
            // The closure rather than the ticks, so a module needed by something needed by a
            // ticked module is held too, however deep the chain runs.
            var included = _modules.WithRequirements(ChosenModules());
            var holders = new Dictionary<ModuleId, List<string>>();

            foreach (var choice in ModuleChoices.Where(choice => included.Contains(choice.Id)))
            {
                foreach (var required in _modules.Describe(choice.Id).Requires)
                {
                    if (!holders.TryGetValue(required, out var names))
                    {
                        holders[required] = names = [];
                    }

                    names.Add(choice.DisplayName);
                }
            }

            foreach (var choice in ModuleChoices)
            {
                choice.Hold(holders.TryGetValue(choice.Id, out var names)
                    ? string.Join(", ", names)
                    : null);
            }
        }
        finally
        {
            _isResolving = false;
        }
    }

    private async Task OpenAsync(CampaignRowViewModel row)
    {
        IsBusy = true;

        try
        {
            await _openCampaign(row.Id);
        }
        catch (CampaignStoreException ex)
        {
            Status = DescribeStoreFailure(ex.Failure, row.Name);
        }
        catch (System.IO.IOException)
        {
            Status = $"Nie udało się odczytać kampanii „{row.Name}”.";
        }
        catch (CampaignUnavailableException)
        {
            Status = $"Kampania „{row.Name}” już nie istnieje.";
            await LoadAsync();
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

    private static string DescribeStoreFailure(CampaignStoreFailure failure, string name) => failure switch
    {
        CampaignStoreFailure.UnsupportedFormatVersion =>
            $"Kampania „{name}” pochodzi z nowszej wersji aplikacji i nie może zostać otwarta.",
        CampaignStoreFailure.Invalid => $"Zapis kampanii „{name}” jest niekompletny.",
        _ => $"Zapis kampanii „{name}” jest uszkodzony."
    };
}
