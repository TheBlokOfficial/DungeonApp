using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using DungeonApp.Models.ContentPacks;
using DungeonApp.Models.ContentPacks.Components;

namespace DungeonApp.Services;

public interface IContentRegistry
{
    IReadOnlyList<ContentPack> GetAllPacks();
    ContentPack? GetPack(string packId);

    ItemTemplate? ResolveItem(string fullTemplateId);
    AdversaryTemplate? ResolveAdversary(string fullTemplateId);
    RarityTemplate? ResolveRarity(string rarityId);
    CategoryTemplate? ResolveCategory(string categoryId);
    FactionTemplate? ResolveFaction(string fullFactionId);
    string? ResolveIconPath(string fullIconId);

    IReadOnlyList<(string FullId, string PackName, ItemTemplate Item)> GetItemsFromPacks(IEnumerable<string> packIds);
    string GetFormattedProperty(string packId, string propertyKey, string rawValue);
    IReadOnlyList<(string FullId, string PackName, ItemTemplate Item)> GetAllItems();

    IReadOnlyList<(string FullId, string PackName, AdversaryTemplate Adversary)> GetAdversariesFromPacks(IEnumerable<string> packIds);
    IReadOnlyList<(string FullId, string PackName, AdversaryTemplate Adversary)> GetAllAdversaries();
}

public class ContentRegistry : IContentRegistry
{
    private readonly Dictionary<string, ContentPack> _packs = new();
    private readonly Dictionary<string, string> _globalIcons = new();
    private readonly Dictionary<string, FactionTemplate> _globalFactions = new();
    private readonly string _packsDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public ContentRegistry()
    {
        _packsDirectory = Path.Combine(AppPaths.UserDataPath, "packs");
        Directory.CreateDirectory(_packsDirectory);
        LoadAllPacks();
    }

    public IReadOnlyList<ContentPack> GetAllPacks()
        => _packs.Values.ToList().AsReadOnly();

    public ContentPack? GetPack(string packId)
        => _packs.GetValueOrDefault(packId);

    public string? ResolveIconPath(string fullIconId)
        => _globalIcons.GetValueOrDefault(fullIconId);
        
    public FactionTemplate? ResolveFaction(string fullFactionId)
        => _globalFactions.GetValueOrDefault(fullFactionId);

    public ItemTemplate? ResolveItem(string fullTemplateId)
    {
        var (packId, itemId) = SplitFullId(fullTemplateId);
        if (packId == null || itemId == null) return null;

        var pack = GetPack(packId);
        var item = pack?.Items.FirstOrDefault(i => i.Id == itemId);
        
        if (item != null && !string.IsNullOrEmpty(item.BaseTemplate))
        {
            var baseItem = ResolveItem(item.BaseTemplate);
            if (baseItem != null)
            {
                var mergedItem = new ItemTemplate
                {
                    Id = item.Id,
                    BaseTemplate = item.BaseTemplate,
                    Name = string.IsNullOrEmpty(item.Name) ? baseItem.Name : item.Name,
                    Description = string.IsNullOrEmpty(item.Description) ? baseItem.Description : item.Description,
                    Type = string.IsNullOrEmpty(item.Type) ? baseItem.Type : item.Type,
                    Rarity = string.IsNullOrEmpty(item.Rarity) ? baseItem.Rarity : item.Rarity,
                    Weight = item.Weight > 0 ? item.Weight : baseItem.Weight,
                    Tags = item.Tags.Count > 0 ? item.Tags : new List<string>(baseItem.Tags),
                    Components = item.Components ?? baseItem.Components
                };
                return mergedItem;
            }
        }
        
        return item;
    }

    public AdversaryTemplate? ResolveAdversary(string fullTemplateId)
    {
        var (packId, advId) = SplitFullId(fullTemplateId);
        if (packId == null || advId == null) return null;

        var pack = GetPack(packId);
        var adv = pack?.Adversaries.FirstOrDefault(m => m.Id == advId);
        
        if (adv != null && !string.IsNullOrEmpty(adv.BaseTemplate))
        {
            var baseAdv = ResolveAdversary(adv.BaseTemplate);
            if (baseAdv != null)
            {
                var mergedAdv = new AdversaryTemplate
                {
                    Id = adv.Id,
                    BaseTemplate = adv.BaseTemplate,
                    Name = string.IsNullOrEmpty(adv.Name) ? baseAdv.Name : adv.Name,
                    Description = string.IsNullOrEmpty(adv.Description) ? baseAdv.Description : adv.Description,
                    Type = string.IsNullOrEmpty(adv.Type) ? baseAdv.Type : adv.Type,
                    Size = string.IsNullOrEmpty(adv.Size) ? baseAdv.Size : adv.Size,
                    Alignment = string.IsNullOrEmpty(adv.Alignment) ? baseAdv.Alignment : adv.Alignment,
                    ChallengeRating = string.IsNullOrEmpty(adv.ChallengeRating) ? baseAdv.ChallengeRating : adv.ChallengeRating,
                    Tags = adv.Tags.Count > 0 ? adv.Tags : new List<string>(baseAdv.Tags),
                    Combat = adv.Combat,
                    Abilities = adv.Abilities,
                    Actions = adv.Actions.Count > 0 ? adv.Actions : new List<ActionDefinition>(baseAdv.Actions)
                };
                return mergedAdv;
            }
        }
        
        return adv;
    }
    
    public RarityTemplate? ResolveRarity(string rarityId)
    {
        if (string.IsNullOrEmpty(rarityId)) return null;
        
        foreach (var pack in _packs.Values)
        {
            if (pack.Rarities != null && pack.Rarities.TryGetValue(rarityId, out var rarity))
            {
                return rarity;
            }
        }
        return null;
    }

    public CategoryTemplate? ResolveCategory(string categoryId)
    {
        if (string.IsNullOrEmpty(categoryId)) return null;

        foreach (var pack in _packs.Values)
        {
            if (pack.Categories != null && pack.Categories.TryGetValue(categoryId, out var cat))
            {
                return cat;
            }
        }
        return null;
    }
    
    public string GetFormattedProperty(string packId, string propertyKey, string rawValue)
    {
        var pack = GetPack(packId);
        PropertyTemplate? template = null;
        
        if (pack != null && pack.PropertyTemplates.TryGetValue(propertyKey, out var localTemplate))
            template = localTemplate;
            
        if (template == null && packId != "core")
        {
            var corePack = GetPack("core");
            if (corePack != null && corePack.PropertyTemplates.TryGetValue(propertyKey, out var coreTemplate))
                template = coreTemplate;
        }
        
        if (template != null && !string.IsNullOrEmpty(template.DisplayFormat))
            return template.DisplayFormat.Replace("%d%", rawValue);
            
        return rawValue;
    }

    public IReadOnlyList<(string FullId, string PackName, ItemTemplate Item)> GetItemsFromPacks(IEnumerable<string> packIds)
    {
        var results = new List<(string, string, ItemTemplate)>();
        foreach (var packId in packIds)
        {
            var pack = GetPack(packId);
            if (pack == null) continue;

            foreach (var item in pack.Items)
            {
                results.Add(($"{pack.Id}:{item.Id}", pack.Name, item));
            }
        }
        return results.AsReadOnly();
    }

    public IReadOnlyList<(string FullId, string PackName, ItemTemplate Item)> GetAllItems()
        => GetItemsFromPacks(_packs.Keys);

    public IReadOnlyList<(string FullId, string PackName, AdversaryTemplate Adversary)> GetAdversariesFromPacks(IEnumerable<string> packIds)
    {
        var results = new List<(string, string, AdversaryTemplate)>();
        foreach (var packId in packIds)
        {
            var pack = GetPack(packId);
            if (pack == null) continue;

            foreach (var adv in pack.Adversaries)
            {
                results.Add(($"{pack.Id}:{adv.Id}", pack.Name, adv));
            }
        }
        return results.AsReadOnly();
    }

    public IReadOnlyList<(string FullId, string PackName, AdversaryTemplate Adversary)> GetAllAdversaries()
        => GetAdversariesFromPacks(_packs.Keys);

    private void LoadAllPacks()
    {
        _packs.Clear();

        var packDirectoriesToScan = new List<string>
        {
            AppPaths.BuiltInPacksPath,
            _packsDirectory
        };

        foreach (var packsDir in packDirectoriesToScan)
        {
            if (!Directory.Exists(packsDir)) continue;

            // Load directories (uncompressed mods)
            var packDirectories = Directory.GetDirectories(packsDir);
            foreach (var dir in packDirectories)
            {
                try
                {
                    var pack = LoadPackFromDirectory(dir);
                    if (pack != null && !string.IsNullOrEmpty(pack.Id))
                    {
                        if (_packs.ContainsKey(pack.Id))
                            Debug.WriteLine($"[ContentRegistry] OSTRZEŻENIE: Nadpisywanie paczki '{pack.Id}' z folderu '{dir}'.");
                        _packs[pack.Id] = pack;
                        Debug.WriteLine($"[ContentRegistry] Załadowano paczkę (Dir) '{pack.Name}' ({pack.Items.Count} przedmiotów, {pack.Adversaries.Count} przeciwników).");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ContentRegistry] Błąd wczytywania paczki z folderu '{dir}': {ex.Message}");
                }
            }

            // Load zip archives (.zip)
            var zipFiles = Directory.GetFiles(packsDir, "*.zip");
            foreach (var zip in zipFiles)
            {
                try
                {
                    var pack = LoadPackFromZip(zip);
                    if (pack != null && !string.IsNullOrEmpty(pack.Id))
                    {
                        if (_packs.ContainsKey(pack.Id))
                            Debug.WriteLine($"[ContentRegistry] OSTRZEŻENIE: Nadpisywanie paczki '{pack.Id}' z pliku '{zip}'.");
                        _packs[pack.Id] = pack;
                        Debug.WriteLine($"[ContentRegistry] Załadowano paczkę (Zip) '{pack.Name}' ({pack.Items.Count} przedmiotów, {pack.Adversaries.Count} przeciwników).");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ContentRegistry] Błąd wczytywania paczki z pliku '{zip}': {ex.Message}");
                }
            }
        }

        if (!_packs.ContainsKey("core"))
        {
            RestoreCorePackFromEmbedded();
            var coreDir = Path.Combine(AppPaths.BuiltInPacksPath, "core");
            var pack = LoadPackFromDirectory(coreDir);
            if (pack != null) _packs[pack.Id] = pack;
        }

        _globalIcons.Clear();
        foreach (var pack in _packs.Values)
        {
            if (pack.Icons != null)
            {
                foreach (var icon in pack.Icons)
                {
                    _globalIcons[$"{pack.Id}:{icon.Key}"] = icon.Value;
                }
            }
            
            if (pack.Factions != null)
            {
                foreach (var faction in pack.Factions)
                {
                    _globalFactions[$"{pack.Id}:{faction.Key}"] = faction.Value;
                }
            }
        }

        Debug.WriteLine($"[ContentRegistry] Załadowano łącznie {_packs.Count} paczek.");
    }

    private ContentPack? LoadPackFromDirectory(string directoryPath)
    {
        var packJsonPath = Path.Combine(directoryPath, "pack.json");
        if (!File.Exists(packJsonPath))
            return null;

        var packJson = File.ReadAllText(packJsonPath);
        var pack = JsonSerializer.Deserialize<ContentPack>(packJson, JsonOptions);
        if (pack == null) return null;

        // Load Items
        var itemsDir = Path.Combine(directoryPath, "items");
        if (Directory.Exists(itemsDir))
        {
            pack.Items = new List<ItemTemplate>();
            foreach (var file in Directory.GetFiles(itemsDir, "*.json"))
            {
                var json = File.ReadAllText(file);
                var item = JsonSerializer.Deserialize<ItemTemplate>(json, JsonOptions);
                if (item != null) pack.Items.Add(item);
            }
        }
        else
        {
            // Fallback for legacy single file
            var itemsPath = Path.Combine(directoryPath, "items.json");
            if (File.Exists(itemsPath))
                pack.Items = JsonSerializer.Deserialize<List<ItemTemplate>>(File.ReadAllText(itemsPath), JsonOptions) ?? new();
        }

        // Load Adversaries
        var advDir = Path.Combine(directoryPath, "adversaries");
        if (Directory.Exists(advDir))
        {
            pack.Adversaries = new List<AdversaryTemplate>();
            foreach (var file in Directory.GetFiles(advDir, "*.json"))
            {
                var json = File.ReadAllText(file);
                var adv = JsonSerializer.Deserialize<AdversaryTemplate>(json, JsonOptions);
                if (adv != null) pack.Adversaries.Add(adv);
            }
        }
        else
        {
            // Fallback for legacy single file
            var advPath = Path.Combine(directoryPath, "adversaries.json");
            if (File.Exists(advPath))
                pack.Adversaries = JsonSerializer.Deserialize<List<AdversaryTemplate>>(File.ReadAllText(advPath), JsonOptions) ?? new();
        }

        var adventuresPath = Path.Combine(directoryPath, "adventures.json");
        if (File.Exists(adventuresPath))
            pack.Adventures = JsonSerializer.Deserialize<List<AdventureTemplate>>(File.ReadAllText(adventuresPath), JsonOptions) ?? new();

        return pack;
    }

    private ContentPack? LoadPackFromZip(string zipFilePath)
    {
        using var archive = ZipFile.OpenRead(zipFilePath);
        
        var packEntry = archive.GetEntry("pack.json");
        if (packEntry == null) return null;

        using (var stream = packEntry.Open())
        using (var reader = new StreamReader(stream))
        {
            var packJson = reader.ReadToEnd();
            var pack = JsonSerializer.Deserialize<ContentPack>(packJson, JsonOptions);
            if (pack == null) return null;

            pack.Items = new List<ItemTemplate>();
            pack.Adversaries = new List<AdversaryTemplate>();
            pack.Adventures = new List<AdventureTemplate>();

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.StartsWith("items/", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    using var s = entry.Open();
                    using var r = new StreamReader(s);
                    var item = JsonSerializer.Deserialize<ItemTemplate>(r.ReadToEnd(), JsonOptions);
                    if (item != null) pack.Items.Add(item);
                }
                else if (entry.FullName.StartsWith("adversaries/", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    using var s = entry.Open();
                    using var r = new StreamReader(s);
                    var adv = JsonSerializer.Deserialize<AdversaryTemplate>(r.ReadToEnd(), JsonOptions);
                    if (adv != null) pack.Adversaries.Add(adv);
                }
                else if (entry.FullName.Equals("items.json", StringComparison.OrdinalIgnoreCase))
                {
                    // Fallback legacy
                    using var s = entry.Open();
                    using var r = new StreamReader(s);
                    var legacyItems = JsonSerializer.Deserialize<List<ItemTemplate>>(r.ReadToEnd(), JsonOptions);
                    if (legacyItems != null) pack.Items.AddRange(legacyItems);
                }
                else if (entry.FullName.Equals("adversaries.json", StringComparison.OrdinalIgnoreCase))
                {
                    // Fallback legacy
                    using var s = entry.Open();
                    using var r = new StreamReader(s);
                    var legacyAdvs = JsonSerializer.Deserialize<List<AdversaryTemplate>>(r.ReadToEnd(), JsonOptions);
                    if (legacyAdvs != null) pack.Adversaries.AddRange(legacyAdvs);
                }
            }
            
            return pack;
        }
    }

    private void RestoreCorePackFromEmbedded()
    {
        var assembly = typeof(ContentRegistry).Assembly;
        var targetDir = Path.Combine(AppPaths.BuiltInPacksPath, "core");
        
        Directory.CreateDirectory(targetDir);

        var resources = assembly.GetManifestResourceNames().Where(n => n.Contains(".Packs.core.")).ToList();
        
        foreach (var resourceName in resources)
        {
            string relative = resourceName.Substring(resourceName.IndexOf(".Packs.core.") + 12);
            
            string finalPath;
            if (relative.StartsWith("items.")) finalPath = Path.Combine(targetDir, "items", relative.Substring(6));
            else if (relative.StartsWith("adversaries.")) finalPath = Path.Combine(targetDir, "adversaries", relative.Substring(12));
            else if (relative.StartsWith("lang.")) finalPath = Path.Combine(targetDir, "lang", relative.Substring(5));
            else finalPath = Path.Combine(targetDir, relative);

            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var fileStream = File.Create(finalPath);
                stream.CopyTo(fileStream);
            }
        }
        Debug.WriteLine("[ContentRegistry] Odzyskano paczkę 'core' z wbudowanych zasobów.");
    }

    private static (string? PackId, string? ItemId) SplitFullId(string fullId)
    {
        if (string.IsNullOrEmpty(fullId)) return (null, null);

        var colonIndex = fullId.IndexOf(':');
        if (colonIndex <= 0 || colonIndex >= fullId.Length - 1) return (null, null);

        return (fullId[..colonIndex], fullId[(colonIndex + 1)..]);
    }
}
