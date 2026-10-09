using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace DungeonApp.Desktop.Workspace.Panels;

/// <summary>
/// A button in a window's header, left of the window's own buttons. The window shows the icon and
/// runs the command; what it does is the body's business.
/// </summary>
public sealed record PanelHeaderAction(string IconResourceKey, string ToolTip, ICommand Command);

/// <summary>
/// What a window body may tell its window beyond the content itself: a title of its own (one
/// descriptor, many windows, each named after what it shows) and header buttons. Both are read
/// again whenever the body raises <see cref="INotifyPropertyChanged.PropertyChanged"/> for them.
/// A body that is not this interface gets the descriptor's title and no buttons.
/// </summary>
public interface IPanelBody : INotifyPropertyChanged
{
    /// <summary>The window's title; null - the descriptor's.</summary>
    string? Title { get; }

    IReadOnlyList<PanelHeaderAction> HeaderActions { get; }
}
