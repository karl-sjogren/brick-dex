using System.Globalization;
using System.IO.Compression;
using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Core.Models.Rebrickable;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrickDex.Core.Services;

public class RebrickableCatalogImportService : IRebrickableCatalogImportService {
    private const string _baseUrl = "https://cdn.rebrickable.com/media/downloads/";

    private readonly IBrickDexContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RebrickableCatalogImportService> _logger;
    private readonly ISearchIndex _searchIndex;

    public RebrickableCatalogImportService(
        IBrickDexContext context,
        HttpClient httpClient,
        ILogger<RebrickableCatalogImportService> logger,
        ISearchIndex searchIndex) {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
        _searchIndex = searchIndex;
    }

    public async Task ImportAllAsync(CancellationToken cancellationToken = default) {
        // Import in order due to FK dependencies
        await ImportThemesAsync(cancellationToken);
        await ImportSetsAsync(cancellationToken);
        await ImportMinifigsAsync(cancellationToken);
        await ImportInventoriesAsync(cancellationToken);
        await ImportInventorySetsAsync(cancellationToken);
        await ImportInventoryMinifigsAsync(cancellationToken);

        // Rebuild search index after import
        _logger.LogInformation("Rebuilding search index after catalog import...");
        await _searchIndex.RebuildIndexAsync(cancellationToken);
        _logger.LogInformation("Search index rebuild complete");
    }

    public async Task ImportThemesAsync(CancellationToken cancellationToken = default) {
        _logger.LogInformation("Importing themes...");

        await using var stream = await DownloadAndExtractCsvAsync("themes.csv.zip", cancellationToken);
        var themes = ParseCsv(stream, record => new RebrickableTheme {
            Id = record.GetField<int>("id"),
            Name = record.GetField<string>("name")!,
            ParentId = record.GetField<string>("parent_id") is { Length: > 0 } parentId
                ? int.Parse(parentId, CultureInfo.InvariantCulture)
                : null
        });

        await UpsertThemesAsync(themes, cancellationToken);

        _logger.LogInformation("Imported {Count} themes", themes.Count);
    }

    public async Task ImportSetsAsync(CancellationToken cancellationToken = default) {
        _logger.LogInformation("Importing sets...");

        await using var stream = await DownloadAndExtractCsvAsync("sets.csv.zip", cancellationToken);
        var sets = ParseCsv(stream, record => new RebrickableSet {
            SetNum = record.GetField<string>("set_num")!,
            Name = record.GetField<string>("name")!,
            Year = record.GetField<int>("year"),
            ThemeId = record.GetField<int>("theme_id"),
            NumParts = record.GetField<int>("num_parts"),
            ImageUrl = record.GetField<string>("img_url") is { Length: > 0 } imgUrl ? imgUrl : null
        });

        await UpsertSetsAsync(sets, cancellationToken);

        _logger.LogInformation("Imported {Count} sets", sets.Count);
    }

    public async Task ImportMinifigsAsync(CancellationToken cancellationToken = default) {
        _logger.LogInformation("Importing minifigs...");

        await using var stream = await DownloadAndExtractCsvAsync("minifigs.csv.zip", cancellationToken);
        var minifigs = ParseCsv(stream, record => new RebrickableMinifig {
            FigNum = record.GetField<string>("fig_num")!,
            Name = record.GetField<string>("name")!,
            NumParts = record.GetField<int>("num_parts"),
            ImageUrl = record.GetField<string>("img_url") is { Length: > 0 } imgUrl ? imgUrl : null
        });

        await UpsertMinifigsAsync(minifigs, cancellationToken);

        _logger.LogInformation("Imported {Count} minifigs", minifigs.Count);
    }

    public async Task ImportInventoriesAsync(CancellationToken cancellationToken = default) {
        _logger.LogInformation("Importing inventories...");

        await using var stream = await DownloadAndExtractCsvAsync("inventories.csv.zip", cancellationToken);
        var inventories = ParseCsv(stream, record => new RebrickableInventory {
            Id = record.GetField<int>("id"),
            Version = record.GetField<int>("version"),
            SetNum = record.GetField<string>("set_num")!
        });

        await UpsertInventoriesAsync(inventories, cancellationToken);

        _logger.LogInformation("Imported {Count} inventories", inventories.Count);
    }

    public async Task ImportInventorySetsAsync(CancellationToken cancellationToken = default) {
        _logger.LogInformation("Importing inventory sets...");

        await using var stream = await DownloadAndExtractCsvAsync("inventory_sets.csv.zip", cancellationToken);
        var inventorySets = ParseCsv(stream, record => new RebrickableInventorySet {
            InventoryId = record.GetField<int>("inventory_id"),
            SetNum = record.GetField<string>("set_num")!,
            Quantity = record.GetField<int>("quantity")
        });

        await UpsertInventorySetsAsync(inventorySets, cancellationToken);

        _logger.LogInformation("Imported {Count} inventory sets", inventorySets.Count);
    }

    public async Task ImportInventoryMinifigsAsync(CancellationToken cancellationToken = default) {
        _logger.LogInformation("Importing inventory minifigs...");

        await using var stream = await DownloadAndExtractCsvAsync("inventory_minifigs.csv.zip", cancellationToken);
        var inventoryMinifigs = ParseCsv(stream, record => new RebrickableInventoryMinifig {
            InventoryId = record.GetField<int>("inventory_id"),
            FigNum = record.GetField<string>("fig_num")!,
            Quantity = record.GetField<int>("quantity")
        });

        await UpsertInventoryMinifigsAsync(inventoryMinifigs, cancellationToken);

        _logger.LogInformation("Imported {Count} inventory minifigs", inventoryMinifigs.Count);
    }

    private async Task<Stream> DownloadAndExtractCsvAsync(string filename, CancellationToken cancellationToken) {
        var url = _baseUrl + filename;
        _logger.LogDebug("Downloading {Url}", url);

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var zipStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var entry = archive.Entries.FirstOrDefault()
            ?? throw new InvalidOperationException($"No entries found in {filename}");

        // Copy to MemoryStream since ZipArchive stream doesn't support seeking
        var memoryStream = new MemoryStream();
        await using var entryStream = await entry.OpenAsync(cancellationToken);
        await entryStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return memoryStream;
    }

    private static List<T> ParseCsv<T>(Stream stream, Func<IReaderRow, T> mapper) {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();

        var items = new List<T>();
        while(csv.Read()) {
            items.Add(mapper(csv));
        }

        return items;
    }

    private async Task UpsertThemesAsync(List<RebrickableTheme> themes, CancellationToken cancellationToken) {
        var existingIds = await _context.RebrickableThemes
            .Select(t => t.Id)
            .ToHashSetAsync(cancellationToken);

        var toAdd = themes.Where(t => !existingIds.Contains(t.Id)).ToList();
        var toUpdate = themes.Where(t => existingIds.Contains(t.Id)).ToList();

        if(toAdd.Count > 0) {
            _context.RebrickableThemes.AddRange(toAdd);
        }

        foreach(var theme in toUpdate) {
            var existing = await _context.RebrickableThemes.FindAsync([theme.Id], cancellationToken);
            if(existing != null) {
                existing.Name = theme.Name;
                existing.ParentId = theme.ParentId;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertSetsAsync(List<RebrickableSet> sets, CancellationToken cancellationToken) {
        var existingKeys = await _context.RebrickableSets
            .Select(s => s.SetNum)
            .ToHashSetAsync(cancellationToken);

        var toAdd = sets.Where(s => !existingKeys.Contains(s.SetNum)).ToList();
        var toUpdate = sets.Where(s => existingKeys.Contains(s.SetNum)).ToList();

        if(toAdd.Count > 0) {
            _context.RebrickableSets.AddRange(toAdd);
        }

        foreach(var set in toUpdate) {
            var existing = await _context.RebrickableSets.FindAsync([set.SetNum], cancellationToken);
            if(existing != null) {
                existing.Name = set.Name;
                existing.Year = set.Year;
                existing.ThemeId = set.ThemeId;
                existing.NumParts = set.NumParts;
                existing.ImageUrl = set.ImageUrl;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertMinifigsAsync(List<RebrickableMinifig> minifigs, CancellationToken cancellationToken) {
        var existingKeys = await _context.RebrickableMinifigs
            .Select(m => m.FigNum)
            .ToHashSetAsync(cancellationToken);

        var toAdd = minifigs.Where(m => !existingKeys.Contains(m.FigNum)).ToList();
        var toUpdate = minifigs.Where(m => existingKeys.Contains(m.FigNum)).ToList();

        if(toAdd.Count > 0) {
            _context.RebrickableMinifigs.AddRange(toAdd);
        }

        foreach(var minifig in toUpdate) {
            var existing = await _context.RebrickableMinifigs.FindAsync([minifig.FigNum], cancellationToken);
            if(existing != null) {
                existing.Name = minifig.Name;
                existing.NumParts = minifig.NumParts;
                existing.ImageUrl = minifig.ImageUrl;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertInventoriesAsync(List<RebrickableInventory> inventories, CancellationToken cancellationToken) {
        // Filter out inventories referencing non-existent sets
        var validSetNums = await _context.RebrickableSets
            .Select(s => s.SetNum)
            .ToHashSetAsync(cancellationToken);

        var originalCount = inventories.Count;
        inventories = inventories.Where(i => validSetNums.Contains(i.SetNum)).ToList();

        if(inventories.Count < originalCount) {
            _logger.LogWarning("Filtered out {Count} inventories referencing non-existent sets",
                originalCount - inventories.Count);
        }

        var existingIds = await _context.RebrickableInventories
            .Select(i => i.Id)
            .ToHashSetAsync(cancellationToken);

        var toAdd = inventories.Where(i => !existingIds.Contains(i.Id)).ToList();
        var toUpdate = inventories.Where(i => existingIds.Contains(i.Id)).ToList();

        if(toAdd.Count > 0) {
            _context.RebrickableInventories.AddRange(toAdd);
        }

        foreach(var inventory in toUpdate) {
            var existing = await _context.RebrickableInventories.FindAsync([inventory.Id], cancellationToken);
            if(existing != null) {
                existing.Version = inventory.Version;
                existing.SetNum = inventory.SetNum;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertInventorySetsAsync(List<RebrickableInventorySet> inventorySets, CancellationToken cancellationToken) {
        // Filter out inventory sets referencing non-existent inventories or sets
        var validInventoryIds = await _context.RebrickableInventories
            .Select(i => i.Id)
            .ToHashSetAsync(cancellationToken);

        var validSetNums = await _context.RebrickableSets
            .Select(s => s.SetNum)
            .ToHashSetAsync(cancellationToken);

        var originalCount = inventorySets.Count;
        inventorySets = inventorySets
            .Where(s => validInventoryIds.Contains(s.InventoryId) && validSetNums.Contains(s.SetNum))
            .ToList();

        if(inventorySets.Count < originalCount) {
            _logger.LogWarning("Filtered out {Count} inventory sets referencing non-existent inventories or sets",
                originalCount - inventorySets.Count);
        }

        var existingKeys = await _context.RebrickableInventorySets
            .Select(s => new { s.InventoryId, s.SetNum })
            .ToListAsync(cancellationToken);

        var existingKeySet = existingKeys.ToHashSet();

        var toAdd = inventorySets.Where(s => !existingKeySet.Contains(new { s.InventoryId, s.SetNum })).ToList();
        var toUpdate = inventorySets.Where(s => existingKeySet.Contains(new { s.InventoryId, s.SetNum })).ToList();

        if(toAdd.Count > 0) {
            _context.RebrickableInventorySets.AddRange(toAdd);
        }

        foreach(var inventorySet in toUpdate) {
            var existing = await _context.RebrickableInventorySets
                .FindAsync([inventorySet.InventoryId, inventorySet.SetNum], cancellationToken);
            if(existing != null) {
                existing.Quantity = inventorySet.Quantity;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertInventoryMinifigsAsync(List<RebrickableInventoryMinifig> inventoryMinifigs, CancellationToken cancellationToken) {
        // Filter out inventory minifigs referencing non-existent inventories or minifigs
        var validInventoryIds = await _context.RebrickableInventories
            .Select(i => i.Id)
            .ToHashSetAsync(cancellationToken);

        var validFigNums = await _context.RebrickableMinifigs
            .Select(m => m.FigNum)
            .ToHashSetAsync(cancellationToken);

        var originalCount = inventoryMinifigs.Count;
        inventoryMinifigs = inventoryMinifigs
            .Where(m => validInventoryIds.Contains(m.InventoryId) && validFigNums.Contains(m.FigNum))
            .ToList();

        if(inventoryMinifigs.Count < originalCount) {
            _logger.LogWarning("Filtered out {Count} inventory minifigs referencing non-existent inventories or minifigs",
                originalCount - inventoryMinifigs.Count);
        }

        var existingKeys = await _context.RebrickableInventoryMinifigs
            .Select(m => new { m.InventoryId, m.FigNum })
            .ToListAsync(cancellationToken);

        var existingKeySet = existingKeys.ToHashSet();

        var toAdd = inventoryMinifigs.Where(m => !existingKeySet.Contains(new { m.InventoryId, m.FigNum })).ToList();
        var toUpdate = inventoryMinifigs.Where(m => existingKeySet.Contains(new { m.InventoryId, m.FigNum })).ToList();

        if(toAdd.Count > 0) {
            _context.RebrickableInventoryMinifigs.AddRange(toAdd);
        }

        foreach(var inventoryMinifig in toUpdate) {
            var existing = await _context.RebrickableInventoryMinifigs
                .FindAsync([inventoryMinifig.InventoryId, inventoryMinifig.FigNum], cancellationToken);
            if(existing != null) {
                existing.Quantity = inventoryMinifig.Quantity;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
