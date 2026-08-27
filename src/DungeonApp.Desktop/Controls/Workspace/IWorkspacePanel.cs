using System.ComponentModel;
using System.Windows.Input;

namespace DungeonApp.Desktop.Controls.Workspace;

/// <summary>
/// What <see cref="WorkspaceSurface"/> needs from an item in order to host it as a floating panel.
/// Declared here rather than in the feature so the control layer stays independent of it: the
/// surface binds to these names, the feature supplies them.
/// <para>
/// The placement members are the panel's <em>effective</em> geometry — what is on screen right now.
/// Their setters must be dumb stores. Snapping, clamping and the desired/effective distinction all
/// live outside them (see <see cref="PanelGeometry"/>); arithmetic in a setter turns the surface's
/// TwoWay binding into an oscillator.
/// </para>
/// </summary>
public interface IWorkspacePanel : INotifyPropertyChanged
{
    string Title { get; }

    string IconResourceKey { get; }

    /// <summary>The module's own view model. Resolved to a view by the app's data templates.</summary>
    object Body { get; }

    double X { get; set; }

    double Y { get; set; }

    double Width { get; set; }

    double Height { get; set; }

    double MinWidth { get; }

    double MinHeight { get; }

    double MaxWidth { get; }

    double MaxHeight { get; }

    int ZOrder { get; }

    bool IsActive { get; }

    PanelDisplayState State { get; set; }

    ICommand ActivateCommand { get; }

    ICommand CloseCommand { get; }

    ICommand MinimizeCommand { get; }

    ICommand ToggleMaximizeCommand { get; }
}
