using System;
using System.Windows.Input;

namespace DungeonApp.Library.Entries.Desktop.Features.ContentTab;

/// <summary>
/// A synchronous command with no parameter - every command a content tab needs (selecting a row,
/// clearing a filter) has nothing to await. Kept local to this feature rather than added to the
/// frame's own <c>DungeonApp.Desktop.ViewModels</c> (which carries <c>AsyncCommand</c>, the
/// asynchronous counterpart this mirrors): krok 10, zlecenie 2's brief reserves any change to the
/// frame beyond <c>Tokens.axaml</c> comments and <c>IGameSystem</c> for a stop-and-report, and this
/// need is fully met without one. Without <c>canExecute</c> the command is always executable; with
/// it, the owner calls <see cref="RaiseCanExecuteChanged"/> whenever what it reads may have changed,
/// so a bound button greys out and comes back (runda 3a: "Wyczyść filtry").
/// </summary>
internal sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => execute();

    /// <summary>Tells bound views to ask <see cref="CanExecute"/> again - a notification, never a mutation.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
