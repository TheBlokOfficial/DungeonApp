using System;
using Avalonia.Controls;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The "Świat kampanii" desk tool's view. Implements <see cref="IDisposable"/> and disposes its own
/// view model on teardown - <see cref="Desktop.Workspace.CampaignWorkspaceViewModel.Dispose"/>
/// (in the shell) only disposes a panel's body when it is itself <see cref="IDisposable"/>, and a bare
/// <see cref="Control"/> is not one, so without this the four event subscriptions
/// <see cref="CampaignEntitiesToolViewModel"/> holds would leak on every campaign close.
/// </summary>
public partial class CampaignEntitiesToolView : UserControl, IDisposable
{
    public CampaignEntitiesToolView()
    {
        InitializeComponent();
    }

    public void Dispose()
    {
        if (DataContext is IDisposable viewModel)
        {
            viewModel.Dispose();
        }
    }
}
