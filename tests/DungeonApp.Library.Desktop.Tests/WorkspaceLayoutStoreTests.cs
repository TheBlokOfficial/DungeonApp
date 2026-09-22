using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonApp.Library.Desktop.Controls.Workspace;
using DungeonApp.Library.Desktop.Features.CampaignWorkspace.Layout;

namespace DungeonApp.Library.Desktop.Tests;

/// <summary>
/// Freezes the store's graceful-degradation promise: reading never throws, and anything that
/// cannot be sensibly interpreted becomes <see cref="WorkspaceLayout.Empty"/> instead. The mapping
/// from on-disk document to <see cref="WorkspaceLayout"/> is written twice in the production file
/// (<c>Load</c> inline, <c>LoadAsync</c> via the private <c>ToLayout</c>), so every case here runs
/// through both entry points via the <paramref name="useAsync"/> theory parameter and
/// <see cref="LoadViaPath"/>.
/// </summary>
public sealed class WorkspaceLayoutStoreTests : IDisposable
{
    private readonly string _directory = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        $"DungeonApp-layout-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task LoadAsync_RestoresSavedLayout()
    {
        var store = new WorkspaceLayoutStore(_directory);
        var expectedPanel = new WorkspacePanelLayout(
            "session.clock",
            "clock-1",
            true,
            PanelDisplayState.Normal,
            2,
            10,
            20,
            300,
            240);
        var expected = new WorkspaceLayout(WorkspaceLayout.CurrentVersion, 1032, 724, [expectedPanel]);
        store.Save("campaign-1", expected);

        var actual = await store.LoadAsync("campaign-1");

        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.SurfaceWidth, actual.SurfaceWidth);
        Assert.Equal(expected.SurfaceHeight, actual.SurfaceHeight);
        Assert.Equal(expectedPanel, Assert.Single(actual.Panels));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Load_ReturnsEmpty_WhenFileDoesNotExist(bool useAsync)
    {
        var store = new WorkspaceLayoutStore(_directory);

        var actual = await LoadViaPath(store, "no-such-workspace", useAsync);

        AssertEmptyLayout(actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Load_ReturnsEmpty_WhenFileIsNotValidJson(bool useAsync)
    {
        WriteRawLayoutFile("corrupt-workspace", "{ this is not json");
        var store = new WorkspaceLayoutStore(_directory);

        var actual = await LoadViaPath(store, "corrupt-workspace", useAsync);

        AssertEmptyLayout(actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Load_ReturnsEmpty_WhenDocumentVersionIsUnknown(bool useAsync)
    {
        // Version 999 never existed - "do not guess, use the defaults" applies even though the
        // JSON itself is otherwise well formed.
        WriteRawLayoutFile("future-version-workspace", """
            {
              "version": 999,
              "surfaceWidth": 800,
              "surfaceHeight": 600,
              "panels": []
            }
            """);
        var store = new WorkspaceLayoutStore(_directory);

        var actual = await LoadViaPath(store, "future-version-workspace", useAsync);

        AssertEmptyLayout(actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Load_CapsPanelsAtMaxPanels_WhenSavedLayoutExceededTheLimit(bool useAsync)
    {
        var store = new WorkspaceLayoutStore(_directory);
        var panels = Enumerable.Range(0, WorkspaceLayout.MaxPanels + 10)
            .Select(index => new WorkspacePanelLayout(
                $"panel.{index}", $"instance-{index}", true, PanelDisplayState.Normal, index, 0, 0, 100, 100))
            .ToList();
        store.Save("overflow-workspace", new WorkspaceLayout(WorkspaceLayout.CurrentVersion, 800, 600, panels));

        var actual = await LoadViaPath(store, "overflow-workspace", useAsync);

        Assert.Equal(WorkspaceLayout.MaxPanels, actual.Panels.Count);
    }

    [Fact]
    public void Save_TruncatesPanelsAtMaxPanels_SoTheFileOnDiskDoesNotGrowWithoutLimit()
    {
        var store = new WorkspaceLayoutStore(_directory);
        var panels = Enumerable.Range(0, WorkspaceLayout.MaxPanels + 10)
            .Select(index => new WorkspacePanelLayout(
                $"panel.{index}", $"instance-{index}", true, PanelDisplayState.Normal, index, 0, 0, 100, 100))
            .ToList();

        store.Save("overflow-workspace", new WorkspaceLayout(WorkspaceLayout.CurrentVersion, 800, 600, panels));

        var path = Path.Combine(_directory, "layouts", "overflow-workspace.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(WorkspaceLayout.MaxPanels, document.RootElement.GetProperty("panels").GetArrayLength());
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("", true)]
    [InlineData("   ", false)]
    [InlineData("   ", true)]
    public async Task Load_SkipsPanel_WhenDescriptorIdIsEmptyOrWhitespace(string blankDescriptorId, bool useAsync)
    {
        var store = new WorkspaceLayoutStore(_directory);
        var blankPanel = new WorkspacePanelLayout(
            blankDescriptorId, "some-key", true, PanelDisplayState.Normal, 0, 0, 0, 100, 100);
        var goodPanel = new WorkspacePanelLayout(
            "session.clock", "clock-1", true, PanelDisplayState.Normal, 1, 0, 0, 100, 100);
        store.Save(
            "blank-descriptor-workspace",
            new WorkspaceLayout(WorkspaceLayout.CurrentVersion, 800, 600, [blankPanel, goodPanel]));

        var actual = await LoadViaPath(store, "blank-descriptor-workspace", useAsync);

        Assert.Equal(goodPanel, Assert.Single(actual.Panels));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Load_ReplacesBlankInstanceKey_WithDescriptorId(bool useAsync)
    {
        var store = new WorkspaceLayoutStore(_directory);
        var panel = new WorkspacePanelLayout(
            "session.clock", "", true, PanelDisplayState.Normal, 0, 0, 0, 100, 100);
        store.Save("blank-instance-key-workspace", new WorkspaceLayout(WorkspaceLayout.CurrentVersion, 800, 600, [panel]));

        var actual = await LoadViaPath(store, "blank-instance-key-workspace", useAsync);

        Assert.Equal("session.clock", Assert.Single(actual.Panels).InstanceKey);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Load_TreatsUnknownPanelState_AsNormal(bool useAsync)
    {
        // State 99 is outside the PanelDisplayState enum - the guard against a hand-edited file, or
        // one written by a newer build that knows a state this one does not.
        WriteRawLayoutFile("unknown-state-workspace", $$"""
            {
              "version": {{WorkspaceLayout.CurrentVersion}},
              "surfaceWidth": 800,
              "surfaceHeight": 600,
              "panels": [
                {
                  "descriptorId": "session.clock",
                  "instanceKey": "clock-1",
                  "isOpen": true,
                  "state": 99,
                  "zOrder": 0,
                  "x": 0,
                  "y": 0,
                  "width": 100,
                  "height": 100
                }
              ]
            }
            """);
        var store = new WorkspaceLayoutStore(_directory);

        var actual = await LoadViaPath(store, "unknown-state-workspace", useAsync);

        Assert.Equal(PanelDisplayState.Normal, Assert.Single(actual.Panels).State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Load_TreatsTwoWorkspaceIds_AsTheSameFile_WhenTheyDifferOnlyByFilteredCharacters(bool useAsync)
    {
        var store = new WorkspaceLayoutStore(_directory);
        var panel = new WorkspacePanelLayout(
            "session.clock", "clock-1", true, PanelDisplayState.Normal, 0, 0, 0, 100, 100);
        // Spaces and "!" are filtered out of a workspace id, so this is saved under the same file as
        // the plain "campaign1" it reads back as below.
        store.Save("campaign 1!", new WorkspaceLayout(WorkspaceLayout.CurrentVersion, 800, 600, [panel]));

        var actual = await LoadViaPath(store, "campaign1", useAsync);

        Assert.Equal(panel, Assert.Single(actual.Panels));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Load_TreatsWorkspaceIdWithNoSurvivingCharacters_AsDefault(bool useAsync)
    {
        var store = new WorkspaceLayoutStore(_directory);
        var panel = new WorkspacePanelLayout(
            "session.clock", "clock-1", true, PanelDisplayState.Normal, 0, 0, 0, 100, 100);
        store.Save("default", new WorkspaceLayout(WorkspaceLayout.CurrentVersion, 800, 600, [panel]));

        // Nothing survives the filter for this id, so it collapses to the same file as "default".
        var actual = await LoadViaPath(store, "!!! ???", useAsync);

        Assert.Equal(panel, Assert.Single(actual.Panels));
    }

    private static Task<WorkspaceLayout> LoadViaPath(WorkspaceLayoutStore store, string workspaceId, bool useAsync) =>
        useAsync ? store.LoadAsync(workspaceId) : Task.FromResult(store.Load(workspaceId));

    private static void AssertEmptyLayout(WorkspaceLayout layout)
    {
        Assert.Equal(WorkspaceLayout.CurrentVersion, layout.Version);
        Assert.Equal(0, layout.SurfaceWidth);
        Assert.Equal(0, layout.SurfaceHeight);
        Assert.Empty(layout.Panels);
    }

    /// <summary>
    /// Writes a layout document straight to disk, bypassing <see cref="WorkspaceLayoutStore.Save"/>,
    /// for content the store itself would never produce (corrupt JSON, an unknown version, an
    /// out-of-range enum value). <paramref name="workspaceId"/> must contain only characters
    /// <c>Sanitize</c> already lets through (ASCII letters, digits, '-', '_') so the path below
    /// matches the store's own <c>GetPath</c> without reaching into that private method.
    /// </summary>
    private string WriteRawLayoutFile(string workspaceId, string json)
    {
        var path = Path.Combine(_directory, "layouts", $"{workspaceId}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
        return path;
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(_directory))
        {
            System.IO.Directory.Delete(_directory, recursive: true);
        }
    }
}
