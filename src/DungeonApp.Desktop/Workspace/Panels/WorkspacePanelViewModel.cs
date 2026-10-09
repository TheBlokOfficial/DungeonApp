using System.Collections.Generic;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Desktop.Workspace.Controls;

namespace DungeonApp.Desktop.Workspace.Panels;

/// <summary>
/// One open panel on the desk.
/// <para>
/// Holds two geometries. <see cref="Desired"/> is what the user last explicitly set with a gesture
/// and is the only one that is persisted. <see cref="X"/>/<see cref="Y"/>/<see cref="Width"/>/
/// <see cref="Height"/> are the effective geometry currently on screen, recomputed from the desired
/// one whenever the desk changes size. Keeping them apart is what makes shrinking and re-growing
/// the window non-destructive: without it, every panel that got tucked in stays tucked in forever.
/// </para>
/// <para>
/// The placement setters are dumb stores and must stay that way. Snapping or clamping in a setter
/// would turn the surface's TwoWay binding into an oscillator - all of that lives in
/// <see cref="PanelGeometry"/> and runs before the value is written.
/// </para>
/// </summary>
public sealed partial class WorkspacePanelViewModel : ObservableObject, IWorkspacePanel
{
    private readonly CampaignWorkspaceViewModel _host;

    public WorkspacePanelViewModel(
        CampaignWorkspaceViewModel host,
        WorkspacePanelDescriptor descriptor,
        string instanceKey,
        PanelPlacement desired)
    {
        _host = host;
        Descriptor = descriptor;
        InstanceKey = instanceKey;
        Desired = desired;
        Body = descriptor.CreateBody(instanceKey);

        X = desired.X;
        Y = desired.Y;
        Width = desired.Width;
        Height = desired.Height;

        // Every panel owns its commands, so each one is parameterless - no command parameter and no
        // closure-capture trick. Built here rather than generated: IWorkspacePanel exposes them as
        // plain ICommand.
        ActivateCommand = new RelayCommand(() => _host.Activate(this));
        MinimizeCommand = new RelayCommand(() => _host.Minimize(this));
        ToggleMaximizeCommand = new RelayCommand(() => _host.ToggleMaximize(this));
        CloseCommand = new RelayCommand(() => _host.Close(this));
        _closeAction = new PanelHeaderAction("DungeonIconClear", "Zamknij", CloseCommand);

        if (Body is IPanelBody body)
        {
            body.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(HeaderActions));
            };
        }
    }

    private readonly PanelHeaderAction _closeAction;

    public WorkspacePanelDescriptor Descriptor { get; }

    public string InstanceKey { get; }

    /// <summary>The placement the user last chose. Only a completed gesture writes this.</summary>
    public PanelPlacement Desired { get; private set; }

    public object Body { get; }

    public string Title => (Body as IPanelBody)?.Title ?? Descriptor.Title;

    /// <summary>The body's header buttons, then the cross of a window that is dismissed rather than minimized.</summary>
    public IReadOnlyList<PanelHeaderAction> HeaderActions
    {
        get
        {
            var own = (Body as IPanelBody)?.HeaderActions ?? [];

            return Descriptor.ClosesPermanently ? [.. own, _closeAction] : own;
        }
    }

    /// <summary>A window that closes permanently has no deck to be minimized to.</summary>
    public bool CanMinimize => !Descriptor.ClosesPermanently;

    public string IconResourceKey => Descriptor.IconResourceKey;

    public double MinWidth => Descriptor.Constraints.MinWidth;

    public double MinHeight => Descriptor.Constraints.MinHeight;

    public double MaxWidth => Descriptor.Constraints.EffectiveMaxWidth;

    public double MaxHeight => Descriptor.Constraints.EffectiveMaxHeight;

    public bool CanMaximize =>
        double.IsPositiveInfinity(Descriptor.Constraints.MaxWidth)
        && double.IsPositiveInfinity(Descriptor.Constraints.MaxHeight);

    [ObservableProperty]
    public partial double X { get; set; }

    [ObservableProperty]
    public partial double Y { get; set; }

    [ObservableProperty]
    public partial double Width { get; set; }

    [ObservableProperty]
    public partial double Height { get; set; }

    [ObservableProperty]
    public partial int ZOrder { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial PanelDisplayState State { get; set; }

    public ICommand ActivateCommand { get; }

    public ICommand MinimizeCommand { get; }

    public ICommand CloseCommand { get; }

    public ICommand ToggleMaximizeCommand { get; }

    /// <summary>Writes the effective geometry. Called by the workspace, never by a gesture directly.</summary>
    public void ApplyEffective(PanelPlacement placement)
    {
        X = placement.X;
        Y = placement.Y;
        Width = placement.Width;
        Height = placement.Height;
    }

    /// <summary>
    /// Promotes what is currently on screen to the desired placement, after a drag or resize ends.
    /// A maximized panel is skipped: its desired placement is precisely the geometry it restores to.
    /// </summary>
    public void CommitGesture()
    {
        if (State == PanelDisplayState.Maximized)
        {
            return;
        }

        Desired = new PanelPlacement(X, Y, Width, Height);
    }

    /// <summary>Used when restoring a saved layout, where the placement did not come from a gesture.</summary>
    public void OverwriteDesired(PanelPlacement placement) => Desired = placement;
}
