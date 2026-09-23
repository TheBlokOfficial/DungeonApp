using System.Linq;
using DungeonApp.Desktop.Content;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// docs/architecture.md, "Pasek boczny: trzy kategorie": "Zakładka kategorii System nie widzi
/// otwartej kampanii." <c>SystemTabContext</c> - the type this test used to scan - is gone: it had
/// become empty once the registry moved out of the frame (docs/tasks.md, "Wyniesienie logiki wpisów z
/// rdzenia do biblioteki"), so the guarantee now lives on <see cref="SystemTabDeclaration"/> itself.
/// <para>
/// A System-category tab's factory is <c>Func&lt;ITabContent&gt;</c> - zero parameters. That is what
/// makes "cannot see a campaign" structural rather than a promise a context class happens to keep:
/// there is nothing here a factory could even accept a campaign, a session or a write door through.
/// This scans the delegate's own signature, the same way the type it replaces scanned a context
/// class's members and constructor.
/// </para>
/// </summary>
public sealed class SystemTabDeclarationIndependenceTests
{
    [Fact]
    public void A_system_tabs_factory_takes_no_parameters()
    {
        var createContentProperty = typeof(SystemTabDeclaration).GetProperty("CreateContent");

        Assert.NotNull(createContentProperty);

        var delegateType = createContentProperty!.PropertyType;
        var invoke = delegateType.GetMethod("Invoke");

        Assert.NotNull(invoke);
        Assert.Empty(invoke!.GetParameters());
    }

    /// <summary>
    /// A second, narrower half of the same guarantee, mirroring what the replaced test asserted of
    /// <c>CampaignTabContext</c> by contrast: a Campaign-category tab's factory, unlike a
    /// System-category one, does take one parameter - proving the zero-parameter shape above is a
    /// deliberate narrowing of this declaration, not an accident of both looking the same.
    /// </summary>
    [Fact]
    public void A_campaign_tabs_factory_takes_exactly_one_parameter()
    {
        var createContentProperty = typeof(CampaignTabDeclaration).GetProperty("CreateContentAsync");

        Assert.NotNull(createContentProperty);

        var invoke = createContentProperty!.PropertyType.GetMethod("Invoke");

        Assert.NotNull(invoke);
        Assert.Single(invoke!.GetParameters());
    }
}
