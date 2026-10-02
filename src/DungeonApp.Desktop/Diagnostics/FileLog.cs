using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace DungeonApp.Desktop.Diagnostics;

/// <summary>
/// Appends timestamped lines to <c>dungeonapp.log</c> in one directory. Once the file grows past
/// <c>maxBytes</c>, it is renamed to <c>dungeonapp.old.log</c> (replacing the previous one) and a
/// fresh file starts, so the log never takes more than about twice that on disk.
/// <para>
/// <see cref="Write"/> never throws: it runs inside exception handlers and in the last moments of a
/// crashing process, and a log that cannot be written must not become a second failure.
/// </para>
/// </summary>
public sealed class FileLog
{
    public const long DefaultMaxBytes = 1024 * 1024;

    private readonly string _directory;
    private readonly string _oldFilePath;
    private readonly long _maxBytes;
    private readonly TimeProvider _time;
    private readonly object _gate = new();

    public FileLog(string directory, long maxBytes = DefaultMaxBytes, TimeProvider? time = null)
    {
        _directory = directory;
        FilePath = Path.Combine(directory, "dungeonapp.log");
        _oldFilePath = Path.Combine(directory, "dungeonapp.old.log");
        _maxBytes = maxBytes;
        _time = time ?? TimeProvider.System;
    }

    public string FilePath { get; }

    public void Write(LogLevel level, string message, Exception? exception)
    {
        try
        {
            var text = Format(level, message, exception);

            lock (_gate)
            {
                Directory.CreateDirectory(_directory);

                var file = new FileInfo(FilePath);
                if (file.Exists && file.Length > _maxBytes)
                {
                    File.Move(FilePath, _oldFilePath, overwrite: true);
                }

                File.AppendAllText(FilePath, text, Encoding.UTF8);
            }
        }
        catch (Exception)
        {
            // See the class remarks: losing a line is the only acceptable outcome of a failed write.
        }
    }

    private string Format(LogLevel level, string message, Exception? exception)
    {
        var builder = new StringBuilder()
            .Append(_time.GetLocalNow().ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture))
            .Append(level == LogLevel.Error ? " [BŁĄD] " : " [INFO] ")
            .AppendLine(message);

        if (exception is not null)
        {
            builder.AppendLine(exception.ToString());
        }

        return builder.ToString();
    }
}
