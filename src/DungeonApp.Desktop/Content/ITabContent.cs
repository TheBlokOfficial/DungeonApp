using System;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// A tab's finished content, handed back by one of a system's tab factories
/// (<see cref="SystemTabDeclaration.CreateContent"/> or <see cref="CampaignTabDeclaration.CreateContentAsync"/>):
/// a ready control plus its own cleanup.
/// <para>
/// <see cref="IDisposable"/> is not an afterthought here - a tab whose content cannot release what
/// it subscribed to does not compile. The shell creates a tab's content once, on first display, and
/// holds it until the tab closes (campaign closed), the GM returns to system selection, or the
/// program exits; each of those releases every live tab the same way, by calling
/// <see cref="IDisposable.Dispose"/> here, never by reaching into what a system built.
/// </para>
/// </summary>
public interface ITabContent : IDisposable
{
    Control Content { get; }
}
