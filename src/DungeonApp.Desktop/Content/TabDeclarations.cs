using System;
using System.Threading.Tasks;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// One tab a system declares in the System sidebar category: a constant triple plus a synchronous
/// factory, read once when the system is chosen (docs/tasks.md, "Etap 1 - projekt styku"). Synchronous
/// because nothing a System-category tab needs today - the content registry, already built by the
/// time a system can be chosen - requires storage IO to draw.
/// </summary>
public sealed record SystemTabDeclaration(
    string Id,
    string Title,
    string IconResourceKey,
    Func<SystemTabContext, ITabContent> CreateContent);

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
