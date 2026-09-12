using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Features.Registry;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// Exercises <see cref="RegistryViewModel"/> against a registry built by actually loading pack files
/// from disk with <see cref="ContentPackLoader"/> - never against a hand-constructed
/// <see cref="ContentRegistry"/> - the same discipline <c>LoadContentPacksStepTests</c> follows,
/// sharing its <see cref="TestPacks"/> helper.
/// <para>
/// Unlike before the content-registry pivot, a card is now a finished <see cref="Avalonia.Controls.Control"/>
/// built by whatever <see cref="Content.IContentPresentation"/> the view model is given
/// (<see cref="FakeContentSet"/> here) - this view model has no way to inspect what is inside it, so
/// these tests only assert that a resolved entry produces one and an unresolved entry does not.
/// </para>
/// </summary>
public sealed class RegistryViewModelTests : IDisposable
{
    private readonly TestPacks _packs = new();

    private static readonly ContentTypeReference IstotaV1 =
        new(ContentId.Create("dnd5e"), ContentId.Create("istota"));

    private static FakeContentSet BuildContentSet() =>
        new(
            ContentId.Create("dnd5e"),
            [new ContentTypeDescriptor(IstotaV1, "Istota", 1)],
            validate: values =>
            {
                var raw = values.Read<System.Collections.Generic.Dictionary<string, JsonElement>>();

                return raw.TryGetValue("odrzuc", out var flag) && flag.ValueKind == JsonValueKind.True
                    ? "Wartości odrzucone przez testowy zestaw."
                    : null;
            });

    private const string PackJson = """
        {
          "formatVersion": 1,
          "id": "bestiariusz",
          "name": "Bestiariusz testowy",
          "version": { "major": 1, "minor": 0 }
        }
        """;

    private const string GoblinJson = """
        {
          "id": "goblin",
          "name": "Goblin",
          "template": "dnd5e:istota",
          "templateVersion": 1,
          "values": { "opis": "Mały rabuś." }
        }
        """;

    // Points at a set this build has never heard of.
    private const string DuchJson = """
        {
          "id": "duch",
          "name": "Duch",
          "template": "widmowa:cos",
          "templateVersion": 1,
          "values": { }
        }
        """;

    // Known type, wrong version.
    private const string ChimeraJson = """
        {
          "id": "chimera",
          "name": "Chimera",
          "template": "dnd5e:istota",
          "templateVersion": 2,
          "values": { }
        }
        """;

    // Known type and version, but the fake content set's validate delegate refuses these values.
    private const string ZepsutyJson = """
        {
          "id": "zepsuty",
          "name": "Zepsuty",
          "template": "dnd5e:istota",
          "templateVersion": 1,
          "values": { "odrzuc": true }
        }
        """;

    public void Dispose() => _packs.Dispose();

    private async Task<RegistryViewModel> BuildViewModelAsync()
    {
        _packs.WriteFile("bestiariusz", "pack.json", PackJson);
        _packs.WriteFile("bestiariusz", "entries/goblin.json", GoblinJson);
        _packs.WriteFile("bestiariusz", "entries/duch.json", DuchJson);
        _packs.WriteFile("bestiariusz", "entries/chimera.json", ChimeraJson);
        _packs.WriteFile("bestiariusz", "entries/zepsuty.json", ZepsutyJson);

        var contentSet = BuildContentSet();
        var registry = await new ContentPackLoader(_packs.Path, contentSet).LoadAsync(CancellationToken.None);
        Assert.Empty(registry.RejectedPacks);

        return new RegistryViewModel(registry, contentSet);
    }

    [Fact]
    public async Task Entries_are_listed_and_sorted_by_name_then_address()
    {
        var viewModel = await BuildViewModelAsync();

        Assert.Equal(
            ["Chimera", "Duch", "Goblin", "Zepsuty"],
            viewModel.Entries.Select(row => row.Name));
    }

    [Fact]
    public async Task Resolved_row_exposes_pack_and_content_type_display_names()
    {
        var viewModel = await BuildViewModelAsync();

        var goblin = viewModel.Entries.Single(row => row.Name == "Goblin");

        Assert.Equal("bestiariusz:goblin", goblin.Address);
        Assert.Equal("Bestiariusz testowy", goblin.PackName);
        Assert.Equal("Istota", goblin.TypeName);
        Assert.False(goblin.IsUnresolved);
    }

    [Fact]
    public async Task Selecting_a_resolved_entry_produces_a_non_empty_card()
    {
        var viewModel = await BuildViewModelAsync();

        viewModel.SelectedEntry = viewModel.Entries.Single(row => row.Name == "Goblin");

        Assert.Null(viewModel.UnresolvedMessage);
        Assert.True(viewModel.HasCard);
        Assert.NotNull(viewModel.Card);
    }

    [Fact]
    public async Task Unresolved_entries_show_no_card_and_a_reason_specific_polish_message()
    {
        var viewModel = await BuildViewModelAsync();

        var duch = viewModel.Entries.Single(row => row.Name == "Duch");
        var chimera = viewModel.Entries.Single(row => row.Name == "Chimera");
        var zepsuty = viewModel.Entries.Single(row => row.Name == "Zepsuty");

        Assert.True(duch.IsUnresolved);
        Assert.True(chimera.IsUnresolved);
        Assert.True(zepsuty.IsUnresolved);
        Assert.Equal(string.Empty, duch.TypeName);

        viewModel.SelectedEntry = duch;
        var missingSetMessage = viewModel.UnresolvedMessage;
        Assert.False(viewModel.HasCard);
        Assert.Null(viewModel.Card);
        Assert.False(string.IsNullOrWhiteSpace(missingSetMessage));
        Assert.Contains("widmowa", missingSetMessage);

        viewModel.SelectedEntry = chimera;
        var versionMismatchMessage = viewModel.UnresolvedMessage;
        Assert.False(viewModel.HasCard);
        Assert.False(string.IsNullOrWhiteSpace(versionMismatchMessage));
        Assert.Contains("istota", versionMismatchMessage);

        viewModel.SelectedEntry = zepsuty;
        var valuesRejectedMessage = viewModel.UnresolvedMessage;
        Assert.False(viewModel.HasCard);
        Assert.False(string.IsNullOrWhiteSpace(valuesRejectedMessage));
        Assert.Contains("Wartości odrzucone przez testowy zestaw.", valuesRejectedMessage);

        // Each reason gets its own wording, never a shared generic fallback text.
        Assert.NotEqual(missingSetMessage, versionMismatchMessage);
        Assert.NotEqual(versionMismatchMessage, valuesRejectedMessage);
        Assert.NotEqual(missingSetMessage, valuesRejectedMessage);
    }

    [Fact]
    public async Task Empty_registry_reports_IsEmpty_without_throwing()
    {
        var contentSet = BuildContentSet();
        var registry = await new ContentPackLoader(_packs.Path, contentSet).LoadAsync(CancellationToken.None);
        var viewModel = new RegistryViewModel(registry, contentSet);

        Assert.True(viewModel.IsEmpty);
        Assert.Empty(viewModel.Entries);
        Assert.Null(viewModel.SelectedEntry);
        Assert.Null(viewModel.Card);
        Assert.Null(viewModel.UnresolvedMessage);
    }

    /// <summary>
    /// An empty registry already explains why the list is bare. Inviting the reader to pick from it
    /// as well puts two contradicting sentences on one screen.
    /// </summary>
    [Fact]
    public async Task Empty_registry_does_not_invite_a_selection()
    {
        var contentSet = BuildContentSet();
        var registry = await new ContentPackLoader(_packs.Path, contentSet).LoadAsync(CancellationToken.None);

        Assert.False(new RegistryViewModel(registry, contentSet).ShowSelectionPrompt);
    }

    [Fact]
    public async Task A_populated_registry_invites_a_selection_until_one_is_made()
    {
        var viewModel = await BuildViewModelAsync();

        Assert.True(viewModel.ShowSelectionPrompt);

        viewModel.SelectedEntry = viewModel.Entries[0];

        Assert.False(viewModel.ShowSelectionPrompt);
    }
}
