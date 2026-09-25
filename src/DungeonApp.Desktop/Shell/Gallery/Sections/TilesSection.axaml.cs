using Avalonia.Controls;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

public partial class TilesSection : UserControl
{
    public TilesSection()
    {
        InitializeComponent();

        TrailBreadcrumbs.Segments = ["Bestiariusz", "Smoki", "Smoki chromatyczne", "Młody czerwony smok"];
        SingleBreadcrumbs.Segments = ["Bestiariusz"];
        string[] narrowTrail = ["Bestiariusz", "Smoki chromatyczne", "Młody smok"];
        NarrowBreadcrumbs.Segments = narrowTrail;
        VeryNarrowBreadcrumbs.Segments = narrowTrail;
    }
}
