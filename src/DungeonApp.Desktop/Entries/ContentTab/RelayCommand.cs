using System;
using System.Windows.Input;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries.ContentTab;

/// <summary>
/// A synchronous command with no parameter - every command a content tab needs (selecting a row,
/// clearing a filter) has nothing to await. It mirrors <c>AsyncCommand</c>, its asynchronous
/// counterpart in <c>DungeonApp.Desktop.ViewModels</c>. Without <c>canExecute</c> the command is
/// always executable; with it, the owner calls <see cref="RaiseCanExecuteChanged"/> whenever what
/// it reads may have changed, so a bound button ("Wyczyść filtry") greys out and comes back.
/// </summary>
internal sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => execute();

    /// <summary>Tells bound views to ask <see cref="CanExecute"/> again - a notification, never a mutation.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
