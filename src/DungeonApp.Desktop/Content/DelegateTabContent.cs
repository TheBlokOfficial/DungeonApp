using System;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// An <see cref="ITabContent"/> for a tab whose content has nothing of its own to release - most of
/// a snapshot screen built once in its view model's constructor, the registry tab being the first
/// example. <paramref name="dispose"/> defaults to doing nothing rather than being left out of the
/// constructor altogether, so a tab that does hold a subscription still fits through this same type
/// instead of a system writing its own one-off wrapper for it.
/// </summary>
public sealed class DelegateTabContent(Control content, Action? dispose = null) : ITabContent
{
    public Control Content { get; } = content;

    public void Dispose() => dispose?.Invoke();
}
