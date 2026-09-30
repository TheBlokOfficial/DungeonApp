using System.IO;

namespace DungeonApp.Core.Systems;

/// <summary>
/// The one place the frame's directory layout for a compiled system's own documents is written down -
/// "Dokumenty\DungeonApp\&lt;system&gt;\packs\" and "Dokumenty\DungeonApp\&lt;system&gt;\campaigns\" -
/// plus the packs shipped together with the program, "&lt;katalog programu&gt;\&lt;system&gt;\packs\".
/// Every method takes a root and a <see cref="SystemId"/>
/// rather than reading the real filesystem itself, so the composition root supplies
/// <c>Environment.GetFolderPath(SpecialFolder.MyDocuments)</c> and <c>AppContext.BaseDirectory</c>
/// exactly once each, and a test supplies a temporary directory instead - never touching the
/// author's real Documents.
/// <para>
/// This lives in Core, not in any one system, because it is the frame that owns the shape of these
/// paths: a system only ever contributes its own <see cref="SystemId"/>, the same identifier it
/// registers itself under everywhere else. Nothing here ever names a specific system.
/// </para>
/// </summary>
public static class SystemDirectories
{
    private const string RootFolderName = "DungeonApp";
    private const string PacksFolderName = "packs";
    private const string CampaignsFolderName = "campaigns";

    /// <summary>The GM's own installed packs for this system - Dokumenty\DungeonApp\&lt;system&gt;\packs\.</summary>
    public static string GmPacks(string documentsPath, SystemId systemId) =>
        Path.Combine(documentsPath, RootFolderName, systemId.Value, PacksFolderName);

    /// <summary>
    /// The packs shipped together with the program for this system -
    /// &lt;katalog programu&gt;\&lt;system&gt;\packs\: the same &lt;system&gt;\packs\ layout as
    /// <see cref="GmPacks"/>, rooted at the program's own directory instead of the GM's Documents.
    /// Read-only: nothing in the program ever writes there.
    /// </summary>
    public static string BundledPacks(string applicationPath, SystemId systemId) =>
        Path.Combine(applicationPath, systemId.Value, PacksFolderName);

    /// <summary>This system's own campaigns - Dokumenty\DungeonApp\&lt;system&gt;\campaigns\.</summary>
    public static string Campaigns(string documentsPath, SystemId systemId) =>
        Path.Combine(documentsPath, RootFolderName, systemId.Value, CampaignsFolderName);
}
