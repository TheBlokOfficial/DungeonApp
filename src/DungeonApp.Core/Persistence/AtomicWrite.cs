using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonApp.Core.Persistence;

/// <summary>
/// Stages one or more files beside their destinations and commits them together with an atomic
/// <see cref="File.Move(string, string, bool)"/> per file. Nothing lands at any destination path
/// until every staged file has serialized cleanly - a write that fails partway through leaves every
/// destination exactly as it was, with only its own temporary file to clean up.
/// </summary>
public sealed class AtomicWrite : IDisposable
{
    /// <summary>
    /// Deterministic on purpose, never a fresh <see cref="Guid"/> per write. A staged file is
    /// normally cleaned up, but a process killed between staging and committing cannot clean up
    /// after itself - and a unique name would leave that orphan on disk forever, one per crash.
    /// Deriving the name from the destination means the next write to the same destination
    /// overwrites the orphan, so the worst case is one stale file per destination rather than an
    /// unbounded pile.
    /// </summary>
    private const string TemporarySuffix = ".writing.tmp";

    private readonly List<(string Temporary, string Destination)> _pending = [];

    /// <summary>
    /// Serializes <paramref name="writeContent"/> into a temporary file beside
    /// <paramref name="destinationPath"/> and records it for <see cref="Commit"/>. Does not create
    /// the destination directory - that is the caller's responsibility.
    /// </summary>
    public void Stage(string destinationPath, Action<Stream> writeContent)
    {
        var temporaryPath = destinationPath + TemporarySuffix;

        using var stream = File.Create(temporaryPath);

        // Recorded before the content is written, not after: a writeContent that throws still leaves
        // a temporary file on disk, and it must stay findable so Dispose can still remove it.
        _pending.Add((temporaryPath, destinationPath));

        writeContent(stream);
    }

    /// <summary>
    /// Asynchronous counterpart of <see cref="Stage"/>.
    /// </summary>
    public async Task StageAsync(
        string destinationPath,
        Func<Stream, CancellationToken, Task> writeContent,
        CancellationToken cancellationToken = default)
    {
        var temporaryPath = destinationPath + TemporarySuffix;

        await using var stream = File.Create(temporaryPath);

        // Recorded before the content is written, not after: a writeContent that throws still leaves
        // a temporary file on disk, and it must stay findable so Dispose can still remove it.
        _pending.Add((temporaryPath, destinationPath));

        await writeContent(stream, cancellationToken);
    }

    /// <summary>
    /// Moves every staged file onto its destination, in the order it was staged - callers rely on
    /// this order, for instance to make a manifest land last so its arrival marks the whole write as
    /// committed. Each move is removed from the pending list as soon as it succeeds, so an exception
    /// partway through leaves the files already moved untouched and only the rest queued for
    /// <see cref="Dispose"/> to clean up. An empty pending list is a valid no-op.
    /// </summary>
    public void Commit()
    {
        while (_pending.Count > 0)
        {
            var (temporary, destination) = _pending[0];
            File.Move(temporary, destination, overwrite: true);
            _pending.RemoveAt(0);
        }
    }

    /// <summary>
    /// Deletes every temporary file still pending - anything <see cref="Commit"/> did not already
    /// move onto its destination. Safe to call more than once.
    /// </summary>
    public void Dispose()
    {
        foreach (var (temporary, _) in _pending)
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }

        _pending.Clear();
    }
}
