using System;
using System.Threading.Tasks;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// One tab a system declares in the System sidebar category: a constant triple plus a synchronous,
/// parameterless factory, read once when the system is chosen. Parameterless is what makes "a
/// System-category tab never sees the open campaign" structural rather than a promise a context
/// class happens to keep today (docs/architecture.md, "Pasek boczny: trzy kategorie") - there is
/// nothing here a factory could even accept a campaign through.
/// </summary>
public sealed record SystemTabDeclaration(
    string Id,
    string Title,
    string IconResourceKey,
    Func<ITabContent> CreateContent);

/// <summary>
/// One tab a system declares in the Campaign sidebar category: a constant triple plus an
/// asynchronous factory, invoked fresh for every campaign the GM opens (never once at system
/// selection - see <see cref="SystemTabDeclaration"/> for the contrast). Asynchronous because
/// building a campaign tab's content can mean reading its own stored state from disk - the desk's
/// saved layout, today.
/// </summary>
public sealed record CampaignTabDeclaration(
    string Id,
    string Title,
    string IconResourceKey,
    Func<CampaignTabContext, Task<ITabContent>> CreateContentAsync);
