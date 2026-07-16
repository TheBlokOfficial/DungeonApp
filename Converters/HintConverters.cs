using Avalonia.Data.Converters;
using Avalonia.Media;
using DungeonApp.Models.Campaigns.Engine.Commands;

namespace DungeonApp.Converters;

/// <summary>
/// Zestaw konwerterów odpowiedzialnych za stylizowanie "hintów" w listach autouzupełniania.
/// Hinty to informacje o argumentach wpisywanych ręcznie (np. &lt;&lt;czas&gt;&gt;), które nie podlegają selekcji.
/// </summary>
public static class HintConverters
{
    public static readonly FuncValueConverter<string?, bool> HintIsInteractable =
        new(str => str == null || !str.StartsWith(CommandNode.HintPrefix));
}
