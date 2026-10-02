using System;
using System.Diagnostics;

namespace DungeonApp.Desktop.Diagnostics;

public enum LogLevel
{
    Info,
    Error
}

/// <summary>
/// The application's one log. Static on purpose: the global exception handlers and the startup
/// steps a system builds for itself have no common owner that could hand them an instance, and
/// threading a logger through every system's constructor would widen the system contract for a
/// concern no system has an opinion about.
/// <para>
/// Writes to <see cref="Debug"/> until the composition root calls <see cref="Use"/> - which is what
/// tests and design-time tooling get, so neither ever writes into the GM's real log directory.
/// </para>
/// </summary>
public static class AppLog
{
    private static volatile Action<LogLevel, string, Exception?> _sink = WriteToDebug;

    /// <summary>Routes every later entry to <paramref name="sink"/>; returns the previous sink.</summary>
    public static Action<LogLevel, string, Exception?> Use(Action<LogLevel, string, Exception?> sink)
    {
        var previous = _sink;
        _sink = sink;
        return previous;
    }

    public static void Info(string message) => _sink(LogLevel.Info, message, null);

    public static void Error(string message, Exception? exception = null) => _sink(LogLevel.Error, message, exception);

    private static void WriteToDebug(LogLevel level, string message, Exception? exception) =>
        Debug.WriteLine(exception is null ? $"[{level}] {message}" : $"[{level}] {message}{Environment.NewLine}{exception}");
}
