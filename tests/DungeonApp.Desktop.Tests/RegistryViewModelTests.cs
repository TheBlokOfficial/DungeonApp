using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Features.Registry;
using DungeonApp.Desktop.Features.Registry.Elements;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// Exercises <see cref="RegistryViewModel"/> and <see cref="CardViewModel"/> against a registry built
/// by actually loading pack files from disk with <see cref="ContentPackLoader"/> - never against a
/// hand-constructed <see cref="ContentRegistry"/> - the same discipline
/// <c>LoadContentPacksStepTests</c> follows, sharing its <see cref="TestPacks"/> helper.
/// </summary>
public sealed class RegistryViewModelTests : IDisposable
{
    private readonly TestPacks _packs = new();

    private const string SystemPackJson = """
        {
          "formatVersion": 1,
          "id": "dnd5e",
          "kind": "system",
          "name": "Piąta edycja",
          "version": { "major": 1, "minor": 0 }
        }
        """;

    // Deliberately close to the "potwor" fixture the task points at: four statblocks, then prose -
    // trimmed to one required field per statblock plus the optional fields these tests need to
    // leave blank, rather than a full monster sheet.
    private const string TemplateJson = """
        {
          "id": "istota",
          "name": "Istota",
          "version": 1,
          "catalogVersion": 1,
          "fields": [
            { "id": "typ",             "label": "Typ",              "type": "text" },
            { "id": "kp",              "label": "KP",               "type": "integer" },
            { "id": "kpZrodlo",        "label": "Źródło KP",        "type": "text",    "required": false },
            { "id": "pz",              "label": "PZ",               "type": "integer" },
            { "id": "umiejetnosci",    "label": "Umiejętności",     "type": "text",    "required": false },
            { "id": "cechySzczegolne", "label": "Cechy szczególne", "type": "text",    "required": false },
            { "id": "opis",            "label": "Opis",             "type": "text",    "required": false }
          ],
          "card": [
            { "element": "statblock", "traits": [ { "field": "typ" } ] },
            {
              "element": "statblock",
              "title": "Obrona",
              "compact": true,
              "traits": [ { "field": "kp", "secondary": "kpZrodlo" }, { "field": "pz" } ]
            },
            { "element": "statblock", "title": "Biegłości", "traits": [ { "field": "umiejetnosci" } ] },
            { "element": "prose", "title": "Cechy szczególne", "field": "cechySzczegolne" },
            { "element": "prose", "title": "Opis", "field": "opis" }
          ]
        }
        """;

    private const string ContentPackJson = """
        {
          "formatVersion": 1,
          "id": "bestiariusz",
          "kind": "content",
          "name": "Bestiariusz testowy",
          "version": { "major": 1, "minor": 0 }
        }
        """;

    // Fully filled: exercises every element, including a trait with a secondary value.
    private const string GoblinJson = """
        {
          "id": "goblin",
          "name": "Goblin",
          "template": "dnd5e:istota",
          "templateVersion": 1,
          "values": {
            "typ": "humanoid",
            "kp": 15,
            "kpZrodlo": "zbroja skórzana, tarcza",
            "pz": 7,
            "umiejetnosci": "Skradanie się +6",
            "cechySzczegolne": "Zwinna ucieczka.",
            "opis": "Mały rabuś."
          }
        }
        """;

    // Leaves every optional field blank: kpZrodlo (secondary), umiejetnosci (the whole third
    // statblock's only trait) and opis (the whole second prose block).
    private const string SzczurJson = """
        {
          "id": "szczur",
          "name": "Szczur",
          "template": "dnd5e:istota",
          "templateVersion": 1,
          "values": {
            "typ": "zwierzę",
            "kp": 10,
            "pz": 2,
            "cechySzczegolne": "Wyczulony węch."
          }
        }
        """;

    private const string DuchJson = """
        {
          "id": "duch",
          "name": "Duch",
          "template": "widmowa:cos",
          "templateVersion": 1
        }
        """;

    private const string SmokJson = """
        {
          "id": "smok",
          "name": "Smok",
          "template": "dnd5e:smok",
          "templateVersion": 1
        }
        """;

    private const string ChimeraJson = """
        {
          "id": "chimera",
          "name": "Chimera",
          "template": "dnd5e:istota",
          "templateVersion": 2
        }
        """;

    public void Dispose() => _packs.Dispose();

    private async Task<RegistryViewModel> BuildViewModelAsync()
    {
        _packs.WriteFile("dnd5e", "pack.json", SystemPackJson);
        _packs.WriteFile("dnd5e", "templates/istota.json", TemplateJson);
        _packs.WriteFile("bestiariusz", "pack.json", ContentPackJson);
        _packs.WriteFile("bestiariusz", "entries/goblin.json", GoblinJson);
        _packs.WriteFile("bestiariusz", "entries/szczur.json", SzczurJson);
        _packs.WriteFile("bestiariusz", "entries/duch.json", DuchJson);
        _packs.WriteFile("bestiariusz", "entries/smok.json", SmokJson);
        _packs.WriteFile("bestiariusz", "entries/chimera.json", ChimeraJson);

        var registry = await new ContentPackLoader(_packs.Path).LoadAsync(CancellationToken.None);
        Assert.Empty(registry.RejectedPacks);

        return new RegistryViewModel(registry);
    }

    [Fact]
    public async Task Entries_are_listed_and_sorted_by_name_then_address()
    {
        var viewModel = await BuildViewModelAsync();

        Assert.Equal(
            ["Chimera", "Duch", "Goblin", "Smok", "Szczur"],
            viewModel.Entries.Select(row => row.Name));
    }

    [Fact]
    public async Task Resolved_row_exposes_pack_and_template_display_names()
    {
        var viewModel = await BuildViewModelAsync();

        var goblin = viewModel.Entries.Single(row => row.Name == "Goblin");

        Assert.Equal("bestiariusz:goblin", goblin.Address);
        Assert.Equal("Bestiariusz testowy", goblin.PackName);
        Assert.Equal("Istota", goblin.TemplateName);
        Assert.False(goblin.IsUnresolved);
    }

    [Fact]
    public async Task Selecting_a_fully_populated_entry_returns_every_element_in_template_order()
    {
        var viewModel = await BuildViewModelAsync();

        viewModel.SelectedEntry = viewModel.Entries.Single(row => row.Name == "Goblin");

        Assert.Null(viewModel.UnresolvedMessage);
        Assert.Equal(5, viewModel.Card.Count);

        var typBlock = Assert.IsType<StatblockElementViewModel>(viewModel.Card[0]);
        var defenseBlock = Assert.IsType<StatblockElementViewModel>(viewModel.Card[1]);
        var skillsBlock = Assert.IsType<StatblockElementViewModel>(viewModel.Card[2]);
        var featuresProse = Assert.IsType<ProseElementViewModel>(viewModel.Card[3]);
        var descriptionProse = Assert.IsType<ProseElementViewModel>(viewModel.Card[4]);

        var typRow = Assert.Single(typBlock.Rows);
        Assert.Equal("Typ", typRow.Label);
        Assert.Equal("humanoid", typRow.Value);

        Assert.Equal("Obrona", defenseBlock.Title);

        // "compact" is a claim about the values, carried through to the view so the element can
        // honour it. Which geometry that becomes is the view's business, not the pack's.
        Assert.True(defenseBlock.IsCompact);
        Assert.False(typBlock.IsCompact);
        Assert.False(skillsBlock.IsCompact);
        Assert.Equal(2, defenseBlock.Rows.Count);

        // The template marks "Obrona" compact; IsCompact carries that straight through from the
        // card element to the view model. The other two blocks left "compact" undeclared, which
        // parses as false.
        Assert.False(typBlock.IsCompact);
        Assert.True(defenseBlock.IsCompact);
        Assert.False(skillsBlock.IsCompact);

        var kpRow = defenseBlock.Rows[0];
        Assert.Equal("KP", kpRow.Label);
        Assert.Equal("15", kpRow.Value);
        Assert.True(kpRow.HasSecondary);
        Assert.Equal("zbroja skórzana, tarcza", kpRow.Secondary);
        // Bracketed for display: without a delimiter "15 zbroja skórzana, tarcza" runs together.
        Assert.Equal("(zbroja skórzana, tarcza)", kpRow.SecondaryDisplay);

        var pzRow = defenseBlock.Rows[1];
        Assert.Equal("7", pzRow.Value);
        Assert.False(pzRow.HasSecondary);

        Assert.Equal("Skradanie się +6", Assert.Single(skillsBlock.Rows).Value);

        Assert.Equal("Cechy szczególne", featuresProse.Title);
        Assert.Equal("Zwinna ucieczka.", featuresProse.Text);
        Assert.Equal("Opis", descriptionProse.Title);
        Assert.Equal("Mały rabuś.", descriptionProse.Text);
    }

    [Fact]
    public async Task Optional_field_without_a_value_drops_its_row_and_an_all_optional_element_disappears_entirely()
    {
        var viewModel = await BuildViewModelAsync();

        viewModel.SelectedEntry = viewModel.Entries.Single(row => row.Name == "Szczur");

        // Only three elements reach the host: the "Biegłości" statblock (its one trait,
        // umiejetnosci, has no value) and the "Opis" prose block (no value either) never appear at
        // all - not as empty elements.
        Assert.Equal(3, viewModel.Card.Count);
        Assert.All(viewModel.Card, element => Assert.True(
            element is StatblockElementViewModel or ProseElementViewModel));

        var defenseBlock = Assert.IsType<StatblockElementViewModel>(viewModel.Card[1]);
        Assert.Equal(2, defenseBlock.Rows.Count);

        // kpZrodlo (kp's secondary) was left blank: the row still renders (kp itself is present),
        // just without a secondary value - it is not skipped outright.
        var kpRow = defenseBlock.Rows[0];
        Assert.Equal("10", kpRow.Value);
        Assert.False(kpRow.HasSecondary);
        Assert.Null(kpRow.Secondary);
        Assert.Null(kpRow.SecondaryDisplay);

        Assert.DoesNotContain(viewModel.Card, element =>
            element is StatblockElementViewModel statblock && statblock.Title == "Biegłości");
        Assert.DoesNotContain(viewModel.Card, element =>
            element is ProseElementViewModel prose && prose.Title == "Opis");
    }

    [Fact]
    public async Task Unresolved_entries_show_an_empty_card_and_a_reason_specific_polish_message()
    {
        var viewModel = await BuildViewModelAsync();

        var duch = viewModel.Entries.Single(row => row.Name == "Duch");
        var smok = viewModel.Entries.Single(row => row.Name == "Smok");
        var chimera = viewModel.Entries.Single(row => row.Name == "Chimera");

        Assert.True(duch.IsUnresolved);
        Assert.True(smok.IsUnresolved);
        Assert.True(chimera.IsUnresolved);
        Assert.Equal(string.Empty, duch.TemplateName);

        viewModel.SelectedEntry = duch;
        var missingPackMessage = viewModel.UnresolvedMessage;
        Assert.Empty(viewModel.Card);
        Assert.False(string.IsNullOrWhiteSpace(missingPackMessage));
        Assert.Contains("widmowa", missingPackMessage);

        viewModel.SelectedEntry = smok;
        var missingTemplateMessage = viewModel.UnresolvedMessage;
        Assert.Empty(viewModel.Card);
        Assert.False(string.IsNullOrWhiteSpace(missingTemplateMessage));
        Assert.Contains("smok", missingTemplateMessage);

        viewModel.SelectedEntry = chimera;
        var versionMismatchMessage = viewModel.UnresolvedMessage;
        Assert.Empty(viewModel.Card);
        Assert.False(string.IsNullOrWhiteSpace(versionMismatchMessage));
        Assert.Contains("istota", versionMismatchMessage);

        // Each of the three EntryUnresolvedReason values gets its own wording, never a shared
        // generic fallback text.
        Assert.NotEqual(missingPackMessage, missingTemplateMessage);
        Assert.NotEqual(missingTemplateMessage, versionMismatchMessage);
        Assert.NotEqual(missingPackMessage, versionMismatchMessage);
    }

    [Fact]
    public async Task Empty_registry_reports_IsEmpty_without_throwing()
    {
        var registry = await new ContentPackLoader(_packs.Path).LoadAsync(CancellationToken.None);
        var viewModel = new RegistryViewModel(registry);

        Assert.True(viewModel.IsEmpty);
        Assert.Empty(viewModel.Entries);
        Assert.Null(viewModel.SelectedEntry);
        Assert.Empty(viewModel.Card);
        Assert.Null(viewModel.UnresolvedMessage);
    }

    /// <summary>
    /// An empty registry already explains why the list is bare. Inviting the reader to pick from it
    /// as well puts two contradicting sentences on one screen.
    /// </summary>
    [Fact]
    public async Task Empty_registry_does_not_invite_a_selection()
    {
        var registry = await new ContentPackLoader(_packs.Path).LoadAsync(CancellationToken.None);

        Assert.False(new RegistryViewModel(registry).ShowSelectionPrompt);
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
