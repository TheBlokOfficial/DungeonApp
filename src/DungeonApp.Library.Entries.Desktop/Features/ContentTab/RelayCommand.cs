using System;
using System.Windows.Input;

namespace DungeonApp.Library.Entries.Desktop.Features.ContentTab;

/// <summary>
/// A synchronous command with no parameter - every command a content tab needs (selecting a row,
/// clearing a filter) has nothing to await. Kept local to this feature rather than added to the
/// frame's own <c>DungeonApp.Desktop.ViewModels</c> (which carries <c>AsyncCommand</c>, the
/// asynchronous counterpart this mirrors): krok 10, zlecenie 2's brief reserves any change to the
/// frame beyond <c>Tokens.axaml</c> comments and <c>IGameSystem</c> for a stop-and-report, and this
/// need is fully met without one. Never raises <see cref="CanExecuteChanged"/> on its own: every
/// current caller is always executable, so there is nothing to invalidate.
/// </summary>
internal sealed class RelayCommand(Action execute) : ICommand
{
    // Explicit, empty accessors rather than a field-like event: every caller here is always
    // executable (see this type's own remarks), so there is nothing that would ever raise this -
    // and a field-like event the class never raises is CS0067, not silence.
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => execute();
}
