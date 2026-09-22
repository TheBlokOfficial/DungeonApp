using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell.SystemSelection;

/// <summary>
/// The fullscreen screen the application starts on: every compiled-in system, and the one gesture
/// that matters here - choosing one. Exists even with a single system installed
/// (docs/architecture.md, "Nawigacja: ekran wyboru systemu i pasek boczny": "Dziś system jest jeden,
/// a ekran wyboru systemu istnieje mimo to").
/// </summary>
public sealed class SystemSelectionViewModel : ObservableObject
{
    private bool _isChoosing;

    public SystemSelectionViewModel(IReadOnlyList<IGameSystem> systems, Func<IGameSystem, Task> choose)
    {
        Systems = [.. systems.Select(system => new SystemOptionViewModel(
            system,
            () => ChooseAsync(system, choose),
            () => !IsChoosing))];
    }

    public IReadOnlyList<SystemOptionViewModel> Systems { get; }

    /// <summary>
    /// True while a choice is being applied - the shelf is being loaded and the first campaign's
    /// tabs are being warmed. Every option's command is disabled meanwhile, so a second click cannot
    /// start a second, overlapping choice.
    /// </summary>
    public bool IsChoosing
    {
        get => _isChoosing;
        private set
        {
            if (SetField(ref _isChoosing, value))
            {
                foreach (var option in Systems)
                {
                    option.ChooseCommand.RaiseCanExecuteChanged();
                }
            }
        }
    }

    private async Task ChooseAsync(IGameSystem system, Func<IGameSystem, Task> choose)
    {
        IsChoosing = true;

        try
        {
            await choose(system);
        }
        finally
        {
            IsChoosing = false;
        }
    }
}

public sealed class SystemOptionViewModel
{
    public SystemOptionViewModel(IGameSystem system, Func<Task> choose, Func<bool> canChoose)
    {
        DisplayName = system.DisplayName;
        ChooseCommand = new AsyncCommand(choose, canChoose);
    }

    public string DisplayName { get; }

    public AsyncCommand ChooseCommand { get; }
}
