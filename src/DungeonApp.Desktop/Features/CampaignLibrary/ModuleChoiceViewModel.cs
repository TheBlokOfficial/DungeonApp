using System;
using DungeonApp.Core.Modules;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignLibrary;

/// <summary>
/// One module the GM can switch on for a new campaign.
/// <para>
/// A module pulled in by another is shown ticked and locked rather than hidden or silently added:
/// what the campaign will contain has to be visible before it is created, because a module can be
/// switched off later but its state is kept forever once it exists.
/// </para>
/// </summary>
public sealed class ModuleChoiceViewModel : ObservableObject
{
    private readonly Action _changed;

    private bool _isChosen;
    private bool _isLocked;
    private string? _requiredBy;

    public ModuleChoiceViewModel(ModuleManifest manifest, bool isChosen, Action changed)
    {
        Id = manifest.Id;
        DisplayName = manifest.DisplayName;
        _isChosen = isChosen;
        _changed = changed;
    }

    public ModuleId Id { get; }

    public string DisplayName { get; }

    public bool IsChosen
    {
        get => _isChosen;
        set
        {
            if (SetField(ref _isChosen, value))
            {
                _changed();
            }
        }
    }

    /// <summary>Ticked because something else needs it, so the GM cannot untick it on its own.</summary>
    public bool IsLocked
    {
        get => _isLocked;
        private set
        {
            if (SetField(ref _isLocked, value))
            {
                RaisePropertyChanged(nameof(CanChange));
            }
        }
    }

    public bool CanChange => !IsLocked;

    /// <summary>Names what is holding it, so a locked tick is explained rather than merely enforced.</summary>
    public string? RequiredBy
    {
        get => _requiredBy;
        private set
        {
            if (SetField(ref _requiredBy, value))
            {
                RaisePropertyChanged(nameof(HasRequiredBy));
            }
        }
    }

    public bool HasRequiredBy => !string.IsNullOrEmpty(RequiredBy);

    /// <summary>
    /// Applied by the owner after it has resolved the whole selection. Ticking here does not call
    /// back, because the selection that produced it has already settled.
    /// </summary>
    public void Hold(string? requiredBy)
    {
        if (requiredBy is not null)
        {
            SetField(ref _isChosen, true, nameof(IsChosen));
        }

        IsLocked = requiredBy is not null;
        RequiredBy = requiredBy is null ? null : $"wymaga tego: {requiredBy}";
    }
}
