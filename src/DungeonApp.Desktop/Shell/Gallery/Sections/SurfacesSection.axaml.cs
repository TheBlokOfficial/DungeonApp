using System.Threading.Tasks;
using Avalonia.Controls;
using DungeonApp.Desktop.Workspace.Leaf;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

public partial class SurfacesSection : UserControl
{
    public SurfacesSection()
    {
        InitializeComponent();
        LeafSample.DataContext = new DeskLeafViewModel(() => Task.CompletedTask);
    }
}
