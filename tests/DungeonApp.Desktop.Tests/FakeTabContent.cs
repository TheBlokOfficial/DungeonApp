using Avalonia.Controls;
using DungeonApp.Desktop.Content;

namespace DungeonApp.Desktop.Tests;

/// <summary>A minimal <see cref="ITabContent"/> that only records whether it was disposed.</summary>
internal sealed class FakeTabContent : ITabContent
{
    public Control Content { get; } = new Panel();

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}
