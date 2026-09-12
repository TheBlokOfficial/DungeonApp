namespace DungeonApp.Architecture.Tests;

/// <summary>
/// Freezes the cases <see cref="VocabularyWordBoundary"/> must get right. Without a frozen test,
/// nobody notices when this pattern quietly regresses back to a plain substring or a plain
/// <c>\b</c> boundary - both look like reasonable simplifications and both are wrong, in opposite
/// directions (see the type doc-comment for which).
/// </summary>
public sealed class VocabularyWordBoundaryTests
{
    [Theory]
    [InlineData("MonsterCardView", "monster", true)]
    [InlineData("monster", "monster", true)]
    [InlineData("monsters", "monster", true)]
    [InlineData("spelled out", "spell", false)]
    [InlineData("spelling", "spell", false)]
    [InlineData("IsMonsterLike", "monster", true)]
    [InlineData("gearbox", "gear", false)]
    public void Matches_the_frozen_cases(string text, string word, bool expected)
    {
        Assert.Equal(expected, VocabularyWordBoundary.IsMatch(text, word));
    }
}
