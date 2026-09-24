using System.Collections.Generic;

namespace DungeonApp.Library.Entries;

/// <summary>
/// One filter's current option list, drawn from the data actually present in a content tab -
/// never a fixed enumeration. An empty <see cref="Options"/> means the filter is unavailable
/// (docs/architecture.md, "Zakładki treści": "filtr bez żadnej opcji jest oznaczony jako
/// niedostępny"), which is exactly what <see cref="IsAvailable"/> reports and a view uses to decide
/// not to draw it.
/// </summary>
public sealed record ContentFilterOptions(string Label, IReadOnlyList<string> Options)
{
    public bool IsAvailable => Options.Count > 0;
}
