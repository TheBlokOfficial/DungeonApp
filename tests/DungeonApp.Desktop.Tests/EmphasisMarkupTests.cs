using System.Linq;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// The one markup a pack's prose may carry. What matters most is what is NOT markup: a stray or
/// empty pair stays in the text as written, so a typo shows up instead of turning a paragraph bold.
/// Emphasized runs are written in brackets below to keep each case on one line.
/// </summary>
public sealed class EmphasisMarkupTests
{
    [Theory]
    [InlineData("Atak: **+4 do trafienia**, zasięg 1,5 m.", "Atak: |[+4 do trafienia]|, zasięg 1,5 m.")]
    [InlineData("**5 (1k6+2)** obrażeń", "[5 (1k6+2)]| obrażeń")]
    [InlineData("**a** i **b**", "[a]| i |[b]")]
    [InlineData("bez znaczników", "bez znaczników")]
    [InlineData("otwarte ** bez pary", "otwarte ** bez pary")]
    [InlineData("**a** i ** bez pary", "[a]| i ** bez pary")]
    [InlineData("pusta para **** zostaje", "pusta para **** zostaje")]
    [InlineData("*pojedyncza gwiazdka*", "*pojedyncza gwiazdka*")]
    [InlineData("", "")]
    public void Parses_only_paired_double_asterisks_around_text(string text, string expected)
    {
        var runs = EmphasisMarkup.Parse(text);

        Assert.Equal(expected, string.Join("|", runs.Select(run => run.IsEmphasized ? $"[{run.Text}]" : run.Text)));
    }
}
