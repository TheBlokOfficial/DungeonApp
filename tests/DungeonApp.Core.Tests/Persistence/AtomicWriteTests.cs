using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DungeonApp.Core.Persistence;

namespace DungeonApp.Core.Tests.Persistence;

public sealed class AtomicWriteTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"dungeonapp-atomicwrite-tests-{Guid.NewGuid():N}");

    public AtomicWriteTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a green test over.
        }
    }

    private string Destination(string name) => Path.Combine(_directory, name);

    private static void WriteText(Stream stream, string content) =>
        stream.Write(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void Leaves_the_destination_untouched_before_Commit()
    {
        var destination = Destination("a.txt");
        File.WriteAllText(destination, "old");

        using var atomic = new AtomicWrite();
        atomic.Stage(destination, stream => WriteText(stream, "new"));

        Assert.Equal("old", File.ReadAllText(destination));
    }

    [Fact]
    public void Does_not_create_a_destination_that_never_existed_before_Commit()
    {
        var destination = Destination("a.txt");

        using var atomic = new AtomicWrite();
        atomic.Stage(destination, stream => WriteText(stream, "new"));

        Assert.False(File.Exists(destination));
    }

    [Fact]
    public void Moves_the_staged_content_onto_the_destination_on_Commit()
    {
        var destination = Destination("a.txt");

        using var atomic = new AtomicWrite();
        atomic.Stage(destination, stream => WriteText(stream, "new"));
        atomic.Commit();

        Assert.Equal("new", File.ReadAllText(destination));
        Assert.Empty(Directory.EnumerateFiles(_directory, "*.writing.tmp"));
    }

    [Fact]
    public void Dispose_without_Commit_leaves_no_temporary_file_and_no_destination()
    {
        var destination = Destination("a.txt");

        var atomic = new AtomicWrite();
        atomic.Stage(destination, stream => WriteText(stream, "new"));

        atomic.Dispose();

        Assert.False(File.Exists(destination));
        Assert.Empty(Directory.EnumerateFiles(_directory, "*.writing.tmp"));
    }

    [Fact]
    public void Commits_every_staged_file_and_none_lands_before_Commit()
    {
        var first = Destination("a.txt");
        var second = Destination("b.txt");

        using var atomic = new AtomicWrite();
        atomic.Stage(first, stream => WriteText(stream, "first"));
        atomic.Stage(second, stream => WriteText(stream, "second"));

        Assert.False(File.Exists(first));
        Assert.False(File.Exists(second));

        atomic.Commit();

        Assert.Equal("first", File.ReadAllText(first));
        Assert.Equal("second", File.ReadAllText(second));
    }

    [Fact]
    public void An_exception_while_staging_a_later_file_leaves_no_temporary_and_no_destination_replaced()
    {
        var first = Destination("a.txt");
        var second = Destination("b.txt");
        File.WriteAllText(second, "old");

        var atomic = new AtomicWrite();
        atomic.Stage(first, stream => WriteText(stream, "first"));

        Assert.Throws<InvalidOperationException>(() =>
            atomic.Stage(second, _ => throw new InvalidOperationException("boom")));

        atomic.Dispose();

        Assert.False(File.Exists(first));
        Assert.Equal("old", File.ReadAllText(second));
        Assert.Empty(Directory.EnumerateFiles(_directory, "*.writing.tmp"));
    }

    [Fact]
    public async Task StageAsync_writes_and_commits_the_same_way_as_Stage()
    {
        var destination = Destination("a.txt");

        using var atomic = new AtomicWrite();
        await atomic.StageAsync(destination, async (stream, token) =>
        {
            var bytes = Encoding.UTF8.GetBytes("async-new");
            await stream.WriteAsync(bytes, token);
        });
        atomic.Commit();

        Assert.Equal("async-new", File.ReadAllText(destination));
        Assert.Empty(Directory.EnumerateFiles(_directory, "*.writing.tmp"));
    }

    /// <summary>
    /// Commit moves files in the order they were staged, and a move that fails partway through does
    /// not undo the moves that already succeeded. This is what lets a manifest staged last in
    /// <c>JsonCampaignRepository</c> mark a whole save as committed: if Commit ever moved files out
    /// of staging order, a torn save would stop being detectable.
    /// <para>
    /// Staged in the same order a real campaign save uses - a data block file, then an instance
    /// file, then the manifest - so this test freezes the three-stage order itself, not just the
    /// two-file case. The middle stage is the one made to fail, which is what proves the manifest
    /// never lands when anything before it does not: a two-file version of this test could not tell
    /// "manifest staged last" apart from "manifest staged second".
    /// </para>
    /// </summary>
    [Fact]
    public void A_failed_move_partway_through_Commit_leaves_the_earlier_moves_in_place()
    {
        var dataBlock = Destination("a.txt");
        var instance = Destination("b.txt");
        var manifest = Destination("c.txt");
        // Occupies the instance destination with a directory, so File.Move onto it fails.
        Directory.CreateDirectory(instance);

        var atomic = new AtomicWrite();
        atomic.Stage(dataBlock, stream => WriteText(stream, "data block"));
        atomic.Stage(instance, stream => WriteText(stream, "instance"));
        atomic.Stage(manifest, stream => WriteText(stream, "manifest"));

        // On this platform, moving onto a path occupied by a directory surfaces as
        // UnauthorizedAccessException rather than IOException - both are the "the move failed"
        // family this test cares about.
        Assert.Throws<UnauthorizedAccessException>(() => atomic.Commit());

        // The data block file was already moved, in staging order, before the instance move failed
        // - and that move is not rolled back.
        Assert.Equal("data block", File.ReadAllText(dataBlock));

        // The manifest, staged last, is never even attempted: Commit moves files strictly in
        // staging order, so a failure on the instance file never lets a later move run.
        Assert.False(File.Exists(manifest));

        atomic.Dispose();

        Assert.Empty(Directory.EnumerateFiles(_directory, "*.writing.tmp"));
    }
}
