using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Desktop.Controls.Workspace;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Layout;

/// <summary>
/// Reads and writes a desk arrangement as JSON, following the same shape as
/// <see cref="DungeonApp.Desktop.Settings.AppSettingsStore"/>: a separate on-disk DTO, hand mapping,
/// and a safe write through a temporary file plus an atomic move.
/// <para>
/// One file per workspace rather than a section inside settings.json, so a corrupt layout can never
/// take the application's own settings down with it. Reading never throws: a layout is a
/// convenience, and losing it must not block startup.
/// </para>
/// </summary>
public sealed class WorkspaceLayoutStore(string directoryPath)
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public WorkspaceLayout Load(string workspaceId)
    {
        var path = GetPath(workspaceId);

        if (!File.Exists(path))
        {
            return WorkspaceLayout.Empty;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var document = JsonSerializer.Deserialize<LayoutDocument>(stream, _serializerOptions);

            // An unknown version is not something to guess at - fall back to the defaults instead.
            if (document is null || document.Version != WorkspaceLayout.CurrentVersion)
            {
                return WorkspaceLayout.Empty;
            }

            var panels = (document.Panels ?? [])
                .Where(panel => !string.IsNullOrWhiteSpace(panel.DescriptorId))
                .Take(WorkspaceLayout.MaxPanels)
                .Select(panel => new WorkspacePanelLayout(
                    panel.DescriptorId,
                    string.IsNullOrWhiteSpace(panel.InstanceKey) ? panel.DescriptorId : panel.InstanceKey,
                    panel.IsOpen,
                    // A hand-edited or newer file can carry a state this build does not know.
                    Enum.IsDefined(panel.State) ? panel.State : PanelDisplayState.Normal,
                    panel.ZOrder,
                    panel.X,
                    panel.Y,
                    panel.Width,
                    panel.Height))
                .ToList();

            return new WorkspaceLayout(document.Version, document.SurfaceWidth, document.SurfaceHeight, panels);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return WorkspaceLayout.Empty;
        }
    }

    /// <summary>
    /// Reads a layout without occupying the UI thread. Startup preparation uses this path so the
    /// first campaign view never pays for file access or JSON metadata generation while it is being
    /// mounted and animated.
    /// </summary>
    public async Task<WorkspaceLayout> LoadAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var path = GetPath(workspaceId);

        if (!File.Exists(path))
        {
            return WorkspaceLayout.Empty;
        }

        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
            var document = await JsonSerializer.DeserializeAsync<LayoutDocument>(
                    stream,
                    _serializerOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            return ToLayout(document);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return WorkspaceLayout.Empty;
        }
    }

    public void Save(string workspaceId, WorkspaceLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var destinationPath = GetPath(workspaceId);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        var document = new LayoutDocument(
            layout.Version,
            layout.SurfaceWidth,
            layout.SurfaceHeight,
            [.. layout.Panels
                .Take(WorkspaceLayout.MaxPanels)
                .Select(panel => new PanelDocument(
                    panel.DescriptorId,
                    panel.InstanceKey,
                    panel.IsOpen,
                    panel.State,
                    panel.ZOrder,
                    panel.X,
                    panel.Y,
                    panel.Width,
                    panel.Height))]);

        var temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            using (var stream = File.Create(temporaryPath))
            {
                JsonSerializer.Serialize(stream, document, _serializerOptions);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private string GetPath(string workspaceId) =>
        Path.Combine(directoryPath, "layouts", $"{Sanitize(workspaceId)}.json");

    private static WorkspaceLayout ToLayout(LayoutDocument? document)
    {
        if (document is null || document.Version != WorkspaceLayout.CurrentVersion)
        {
            return WorkspaceLayout.Empty;
        }

        var panels = (document.Panels ?? [])
            .Where(panel => !string.IsNullOrWhiteSpace(panel.DescriptorId))
            .Take(WorkspaceLayout.MaxPanels)
            .Select(panel => new WorkspacePanelLayout(
                panel.DescriptorId,
                string.IsNullOrWhiteSpace(panel.InstanceKey) ? panel.DescriptorId : panel.InstanceKey,
                panel.IsOpen,
                Enum.IsDefined(panel.State) ? panel.State : PanelDisplayState.Normal,
                panel.ZOrder,
                panel.X,
                panel.Y,
                panel.Width,
                panel.Height))
            .ToList();

        return new WorkspaceLayout(document.Version, document.SurfaceWidth, document.SurfaceHeight, panels);
    }

    /// <summary>
    /// A workspace identifier becomes a file name. Today it is a constant, but it becomes a campaign
    /// identifier later, so it is filtered rather than trusted.
    /// </summary>
    private static string Sanitize(string workspaceId)
    {
        var safe = new string([.. workspaceId.Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')]);
        return safe.Length == 0 ? "default" : safe;
    }

    private sealed record LayoutDocument(
        int Version,
        double SurfaceWidth,
        double SurfaceHeight,
        IReadOnlyList<PanelDocument>? Panels);

    private sealed record PanelDocument(
        string DescriptorId,
        string InstanceKey,
        bool IsOpen,
        PanelDisplayState State,
        int ZOrder,
        double X,
        double Y,
        double Width,
        double Height);
}
