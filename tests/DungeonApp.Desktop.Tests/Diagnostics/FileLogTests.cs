using System;
using System.IO;
using DungeonApp.Desktop.Diagnostics;
using DungeonApp.Testing;

namespace DungeonApp.Desktop.Tests.Diagnostics;

public sealed class FileLogTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 14, 30, 5, TimeSpan.Zero);

    private readonly TemporaryLogs _logs = new();

    public void Dispose() => _logs.Dispose();

    [Fact]
    public void Writes_a_timestamped_line_with_its_level_and_the_exception()
    {
        var log = new FileLog(Path.Combine(_logs.Path, "logs"), time: new FixedTimeProvider(Now));

        log.Write(LogLevel.Info, "Start programu.", null);
        log.Write(LogLevel.Error, "Krok zawiódł.", new InvalidOperationException("zepsute"));

        var text = File.ReadAllText(log.FilePath);
        Assert.Contains("[INFO] Start programu.", text);
        Assert.Contains("[BŁĄD] Krok zawiódł.", text);
        Assert.Contains("System.InvalidOperationException: zepsute", text);
        Assert.StartsWith(Now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"), text);
    }

    [Fact]
    public void A_file_past_the_limit_becomes_the_old_log_and_a_fresh_one_starts()
    {
        var log = new FileLog(_logs.Path, maxBytes: 100);
        var oldPath = Path.Combine(_logs.Path, "dungeonapp.old.log");
        File.WriteAllText(oldPath, "najstarszy");

        log.Write(LogLevel.Info, new string('a', 150), null);
        log.Write(LogLevel.Info, "nowy", null);

        Assert.Contains(new string('a', 150), File.ReadAllText(oldPath));
        Assert.DoesNotContain("najstarszy", File.ReadAllText(oldPath));
        var current = File.ReadAllText(log.FilePath);
        Assert.Contains("nowy", current);
        Assert.DoesNotContain("aaaa", current);
    }

    [Fact]
    public void A_directory_that_cannot_be_created_loses_the_line_without_throwing()
    {
        // A file standing where the log directory should be makes every write fail.
        var blocked = Path.Combine(_logs.Path, "zajete");
        File.WriteAllText(blocked, string.Empty);
        var log = new FileLog(blocked);

        var thrown = Record.Exception(() => log.Write(LogLevel.Error, "nie trafi do pliku", new Exception()));

        Assert.Null(thrown);
        Assert.False(File.Exists(log.FilePath));
    }
}
