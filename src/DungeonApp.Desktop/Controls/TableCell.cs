using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// <see cref="Table"/> cell: padding, content alignment (HorizontalContentAlignment), highlight
/// background (Background supplied by the view - fills the cell up to its lines). Cell draws
/// its own lines, but table determines thickness from cell position (<see cref="Table"/>). Appearance
/// and classes (.header - header row, .mono - numbers in the numeric font) belong to the shell theme
/// (Themes/Controls/Table.axaml).
/// </summary>
public sealed class TableCell : ContentControl
{
}
