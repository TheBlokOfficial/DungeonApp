using System;
using System.Linq;
using System.Reflection;
using DungeonApp.Desktop.Content;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// docs/tasks.md, "Testy": a System-category tab's context exposes nothing from a campaign
/// (docs/architecture.md, "Pasek boczny: trzy kategorie": "Zakładka kategorii System nie widzi
/// otwartej kampanii"). Unlike <see cref="CampaignTabContext"/>, <see cref="SystemTabContext"/> has
/// no constructor parameter and no property that could leak one in - this asserts that stays true by
/// scanning its public surface, rather than trusting a reviewer to notice a new field added later.
/// </summary>
public sealed class SystemTabContextIndependenceTests
{
    [Fact]
    public void SystemTabContext_exposes_no_member_naming_a_campaign()
    {
        var type = typeof(SystemTabContext);

        var offendingMembers = type
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(member => member is not ConstructorInfo)
            .Where(NamesACampaign)
            .Select(member => member.Name)
            .ToArray();

        Assert.True(
            offendingMembers.Length == 0,
            "SystemTabContext must expose nothing from a campaign, but found: "
            + string.Join(", ", offendingMembers));
    }

    /// <summary>
    /// A second, narrower half of the same guarantee: even the constructor's own parameters must
    /// never accept a campaign, a session, or the write door only an open campaign can hand out -
    /// there being nothing to construct one <em>from</em> is what makes the property structural
    /// rather than a promise this type's author happened to keep.
    /// </summary>
    [Fact]
    public void SystemTabContext_constructor_accepts_nothing_naming_a_campaign()
    {
        var constructor = Assert.Single(typeof(SystemTabContext).GetConstructors());

        var offendingParameters = constructor.GetParameters()
            .Where(parameter => VocabularyWordBoundary.IsMatch(parameter.ParameterType.Name, "campaign")
                || VocabularyWordBoundary.IsMatch(parameter.ParameterType.Name, "session"))
            .Select(parameter => parameter.ParameterType.Name)
            .ToArray();

        Assert.True(
            offendingParameters.Length == 0,
            "SystemTabContext's constructor must accept nothing naming a campaign, but found: "
            + string.Join(", ", offendingParameters));
    }

    private static bool NamesACampaign(MemberInfo member)
    {
        if (VocabularyWordBoundary.IsMatch(member.Name, "campaign"))
        {
            return true;
        }

        var memberType = member switch
        {
            PropertyInfo property => property.PropertyType,
            MethodInfo method => method.ReturnType,
            _ => null,
        };

        return memberType is not null && VocabularyWordBoundary.IsMatch(memberType.Name, "campaign");
    }
}
