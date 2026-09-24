using System.IO;

namespace DungeonApp.Core.Systems;

/// <summary>
/// The one place the frame's directory layout for a compiled system's own documents is written down -
/// docs/architecture.md, "Gdzie mieszka stan": "Dokumenty\DungeonApp\&lt;system&gt;\packs\" and
/// "Dokumenty\DungeonApp\&lt;system&gt;\campaigns\". Every method takes a root and a <see cref="SystemId"/>
/// rather than reading the real filesystem itself, so the composition root supplies
/// <c>Environment.GetFolderPath(SpecialFolder.MyDocuments)</c> (or <c>AppContext.BaseDirectory</c> for
/// <see cref="BundledPacks"/>) exactly once, and a test supplies a temporary directory instead - never
/// touching the author's real Documents.
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

    /// <summary>This system's own campaigns - Dokumenty\DungeonApp\&lt;system&gt;\campaigns\.</summary>
    public static string Campaigns(string documentsPath, SystemId systemId) =>
        Path.Combine(documentsPath, RootFolderName, systemId.Value, CampaignsFolderName);

    /// <summary>
    /// Packs the system itself ships with, read-only, updated with the application's own release
    /// (docs/architecture.md, "Paczki, wczytywanie, bezpieczeństwo": "System może dostarczać paczki
    /// razem ze sobą"). These sit beside the program rather than under the user's documents, so the
    /// root here is the application's own base directory, not Documents - &lt;base&gt;\&lt;system&gt;\packs\.
    /// </summary>
    public static string BundledPacks(string applicationBaseDirectory, SystemId systemId) =>
        Path.Combine(applicationBaseDirectory, systemId.Value, PacksFolderName);
}
