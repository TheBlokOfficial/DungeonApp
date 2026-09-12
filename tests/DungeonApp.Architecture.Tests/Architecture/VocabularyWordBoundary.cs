using System.Text.RegularExpressions;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// The word-boundary rule the vocabulary scan applies to every source line. Plain substring
/// matching catches "spelled" while looking for "spell"; a plain regex word boundary (<c>\b</c>)
/// misses "MonsterCardView" entirely, because the whole identifier is one continuous run of word
/// characters with no boundary after "Monster". This respects the CamelCase boundary instead of
/// either:
/// <list type="bullet">
/// <item>a match is rejected if the character right after it is a lowercase letter - this is what
/// rejects "spelled" and "spelling" while still accepting "MonsterCardView", where the next
/// character is the uppercase start of the next word;</item>
/// <item>a match is rejected if the character right before it is a letter, <i>unless</i> that letter
/// is lowercase and the match itself starts with an uppercase letter - which is exactly the shape of
/// a CamelCase word boundary ("IsMonsterLike"), and is the only situation in which a directly
/// adjacent letter is not a sign that the match is stuck inside a longer word.</item>
/// </list>
/// <para>
/// This is a hand-rolled scan rather than a single regex with lookaround, because the boundary
/// condition needs to know the actual letter-case of the specific characters found in the text, and
/// a regex character class cannot express that once <see cref="RegexOptions.IgnoreCase"/> is in
/// play: the option folds a class like <c>[a-z]</c> (or the Unicode "lowercase letter" category) to
/// also match uppercase letters, which silently destroys the asymmetry this rule depends on. That
/// was verified empirically before writing this, not assumed - see the frozen cases in
/// <see cref="VocabularyWordBoundaryTests"/>.
/// </para>
/// </summary>
internal static class VocabularyWordBoundary
{
    public static bool IsMatch(string text, string word)
    {
        var pattern = Regex.Escape(word) + "s?";

        foreach (Match match in Regex.Matches(text, pattern, RegexOptions.IgnoreCase))
        {
            var start = match.Index;
            var end = match.Index + match.Length;

            var after = end < text.Length ? text[end] : (char?)null;
            if (after is { } nextChar && char.IsLower(nextChar))
            {
                continue;
            }

            var before = start > 0 ? text[start - 1] : (char?)null;
            var matchStartsUpper = char.IsUpper(text[start]);
            var leftBoundaryOk = before is not { } previousChar
                || !char.IsLetter(previousChar)
                || (matchStartsUpper && char.IsLower(previousChar));

            if (leftBoundaryOk)
            {
                return true;
            }
        }

        return false;
    }
}
