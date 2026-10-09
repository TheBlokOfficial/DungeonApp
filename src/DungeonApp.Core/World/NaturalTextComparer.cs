using System;
using System.Collections.Generic;
using System.Globalization;

namespace DungeonApp.Core.World;

/// <summary>
/// Orders names the way a GM reads them: Polish collation without regard to case, and a run of
/// digits as a number, so "Goblin 2" comes before "Goblin 10".
/// </summary>
public sealed class NaturalTextComparer : IComparer<string>
{
    private static readonly CompareInfo Polish = new CultureInfo("pl-PL").CompareInfo;

    public static NaturalTextComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        int i = 0, j = 0;

        while (i < x.Length && j < y.Length)
        {
            var runX = ReadRun(x, i);
            var runY = ReadRun(y, j);
            i += runX.Length;
            j += runY.Length;

            var result = char.IsDigit(runX[0]) && char.IsDigit(runY[0])
                ? CompareNumbers(runX, runY)
                : Polish.Compare(runX, runY, CompareOptions.IgnoreCase);

            if (result != 0)
            {
                return result;
            }
        }

        // The one with text left over is the longer, so it goes later.
        return (x.Length - i).CompareTo(y.Length - j);
    }

    private static string ReadRun(string text, int start)
    {
        var digits = char.IsDigit(text[start]);
        var end = start;

        while (end < text.Length && char.IsDigit(text[end]) == digits)
        {
            end++;
        }

        return text[start..end];
    }

    private static int CompareNumbers(string x, string y)
    {
        var a = x.TrimStart('0');
        var b = y.TrimStart('0');

        return a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(a, b);
    }
}
