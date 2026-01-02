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
    private const int _batchSize = 2000;

    private readonly IBrickDexContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RebrickableCatalogImportService> _logger;

    public RebrickableCatalogImportService(
        IBrickDexContext context,
        HttpClient httpClient,
        ILogger<RebrickableCatalogImportService> logger) {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task ImportAllAsync(CancellationToken cancellationToken = default) {
        // Import in order due to FK dependencies
        await ImportThemesAsync(cancellationToken);
        await ImportSetsAsync(cancellationToken);
        await ImportMinifigsAsync(cancellationToken);
        await ImportInventoriesAsync(cancellationToken);
        await ImportInventorySetsAsync(cancellationToken);
        await ImportInventoryMinifigsAsync(cancellationToken);
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
        foreach(var batch in themes.Chunk(_batchSize)) {
            var batchIds = batch.Select(t => t.Id).ToList();

            var existingThemes = await _context.RebrickableThemes
                .Where(t => batchIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, cancellationToken);

            var toAdd = new List<RebrickableTheme>();

            foreach(var theme in batch) {
                if(existingThemes.TryGetValue(theme.Id, out var existing)) {
                    existing.Name = theme.Name;
                    existing.ParentId = theme.ParentId;
                } else {
                    toAdd.Add(theme);
                }
            }

            if(toAdd.Count > 0) {
                _context.RebrickableThemes.AddRange(toAdd);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task UpsertSetsAsync(List<RebrickableSet> sets, CancellationToken cancellationToken) {
        foreach(var batch in sets.Chunk(_batchSize)) {
            var batchSetNums = batch.Select(s => s.SetNum).ToList();

            var existingSets = await _context.RebrickableSets
                .Where(s => batchSetNums.Contains(s.SetNum))
                .ToDictionaryAsync(s => s.SetNum, cancellationToken);

            var toAdd = new List<RebrickableSet>();

            foreach(var set in batch) {
                if(existingSets.TryGetValue(set.SetNum, out var existing)) {
                    existing.Name = set.Name;
                    existing.Year = set.Year;
                    existing.ThemeId = set.ThemeId;
                    existing.NumParts = set.NumParts;
                    existing.ImageUrl = set.ImageUrl;
                } else {
                    toAdd.Add(set);
                }
            }

            if(toAdd.Count > 0) {
                _context.RebrickableSets.AddRange(toAdd);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task UpsertMinifigsAsync(List<RebrickableMinifig> minifigs, CancellationToken cancellationToken) {
        foreach(var batch in minifigs.Chunk(_batchSize)) {
            var batchFigNums = batch.Select(m => m.FigNum).ToList();

            var existingMinifigs = await _context.RebrickableMinifigs
                .Where(m => batchFigNums.Contains(m.FigNum))
                .ToDictionaryAsync(m => m.FigNum, cancellationToken);

            var toAdd = new List<RebrickableMinifig>();

            foreach(var minifig in batch) {
                if(existingMinifigs.TryGetValue(minifig.FigNum, out var existing)) {
                    existing.Name = minifig.Name;
                    existing.NumParts = minifig.NumParts;
                    existing.ImageUrl = minifig.ImageUrl;
                } else {
                    toAdd.Add(minifig);
                }
            }

            if(toAdd.Count > 0) {
                _context.RebrickableMinifigs.AddRange(toAdd);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
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

        foreach(var batch in inventories.Chunk(_batchSize)) {
            var batchIds = batch.Select(i => i.Id).ToList();

            var existingInventories = await _context.RebrickableInventories
                .Where(i => batchIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, cancellationToken);

            var toAdd = new List<RebrickableInventory>();

            foreach(var inventory in batch) {
                if(existingInventories.TryGetValue(inventory.Id, out var existing)) {
                    existing.Version = inventory.Version;
                    existing.SetNum = inventory.SetNum;
                } else {
                    toAdd.Add(inventory);
                }
            }

            if(toAdd.Count > 0) {
                _context.RebrickableInventories.AddRange(toAdd);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
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

        foreach(var batch in inventorySets.Chunk(_batchSize)) {
            var batchInventoryIds = batch.Select(s => s.InventoryId).ToHashSet();

            var existingInventorySets = await _context.RebrickableInventorySets
                .Where(s => batchInventoryIds.Contains(s.InventoryId))
                .ToDictionaryAsync(s => (s.InventoryId, s.SetNum), cancellationToken);

            var toAdd = new List<RebrickableInventorySet>();

            foreach(var inventorySet in batch) {
                var key = (inventorySet.InventoryId, inventorySet.SetNum);
                if(existingInventorySets.TryGetValue(key, out var existing)) {
                    existing.Quantity = inventorySet.Quantity;
                } else {
                    toAdd.Add(inventorySet);
                }
            }

            if(toAdd.Count > 0) {
                _context.RebrickableInventorySets.AddRange(toAdd);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
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

        foreach(var batch in inventoryMinifigs.Chunk(_batchSize)) {
            var batchInventoryIds = batch.Select(m => m.InventoryId).ToHashSet();

            var existingInventoryMinifigs = await _context.RebrickableInventoryMinifigs
                .Where(m => batchInventoryIds.Contains(m.InventoryId))
                .ToDictionaryAsync(m => (m.InventoryId, m.FigNum), cancellationToken);

            var toAdd = new List<RebrickableInventoryMinifig>();

            foreach(var inventoryMinifig in batch) {
                var key = (inventoryMinifig.InventoryId, inventoryMinifig.FigNum);
                if(existingInventoryMinifigs.TryGetValue(key, out var existing)) {
                    existing.Quantity = inventoryMinifig.Quantity;
                } else {
                    toAdd.Add(inventoryMinifig);
                }
            }

            if(toAdd.Count > 0) {
                _context.RebrickableInventoryMinifigs.AddRange(toAdd);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
