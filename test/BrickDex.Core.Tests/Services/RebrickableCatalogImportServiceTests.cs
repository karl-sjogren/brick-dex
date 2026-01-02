using System.IO.Compression;
using System.Net;
using System.Text;
using BrickDex.Core.Contracts;
using BrickDex.Core.Models.Rebrickable;
using BrickDex.Core.Services;
using BrickDex.TestHelpers;
using BrickDex.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Core.Tests.Services;

public class RebrickableCatalogImportServiceTests : IDisposable {
    private readonly BrickDexContext _context;
    private readonly FakeHttpMessageHandler _httpHandler;
    private readonly IRebrickableCatalogImportService _sut;

    public RebrickableCatalogImportServiceTests() {
        var options = new DbContextOptionsBuilder<BrickDexContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BrickDexContext(options);
        _httpHandler = new FakeHttpMessageHandler();

        var httpClient = new HttpClient(_httpHandler) {
            BaseAddress = new Uri("https://cdn.rebrickable.com/media/downloads/")
        };

        var logger = new NullLogger<RebrickableCatalogImportService>();
        var searchIndex = A.Fake<ISearchIndex>();
        _sut = new RebrickableCatalogImportService(_context, httpClient, logger, searchIndex);
    }

    public void Dispose() {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    // Theme import tests

    [Fact]
    public async Task ImportThemesAsync_ParsesThemesCorrectlyAsync() {
        // Arrange
        var csv = await Resources.GetStringAsync("themes.csv");
        _httpHandler.SetupZipResponse("themes.csv.zip", "themes.csv", csv);

        // Act
        await _sut.ImportThemesAsync(TestCancellationToken);

        // Assert
        var themes = await _context.RebrickableThemes.ToListAsync(TestCancellationToken);
        themes.Count.ShouldBe(4);

        var technic = themes.Single(t => t.Id == 1);
        technic.Name.ShouldBe("Technic");
        technic.ParentId.ShouldBeNull();

        var starWarsUcs = themes.Single(t => t.Id == 171);
        starWarsUcs.Name.ShouldBe("Star Wars Ultimate Collector Series");
        starWarsUcs.ParentId.ShouldBe(158);
    }

    [Fact]
    public async Task ImportThemesAsync_UpdatesExistingThemesAsync() {
        // Arrange
        _context.RebrickableThemes.Add(new RebrickableTheme { Id = 1, Name = "Old Name" });
        await _context.SaveChangesAsync(TestCancellationToken);

        var csv = await Resources.GetStringAsync("themes.csv");
        _httpHandler.SetupZipResponse("themes.csv.zip", "themes.csv", csv);

        // Act
        await _sut.ImportThemesAsync(TestCancellationToken);

        // Assert
        var theme = await _context.RebrickableThemes.FindAsync([1], TestCancellationToken);
        theme!.Name.ShouldBe("Technic");
    }

    // Set import tests

    [Fact]
    public async Task ImportSetsAsync_ParsesSetsCorrectlyAsync() {
        // Arrange - need themes first due to FK
        await SeedThemesAsync();

        var csv = await Resources.GetStringAsync("sets.csv");
        _httpHandler.SetupZipResponse("sets.csv.zip", "sets.csv", csv);

        // Act
        await _sut.ImportSetsAsync(TestCancellationToken);

        // Assert
        var sets = await _context.RebrickableSets.ToListAsync(TestCancellationToken);
        sets.Count.ShouldBe(4);

        var millenniumFalcon = sets.Single(s => s.SetNum == "75192-1");
        millenniumFalcon.Name.ShouldBe("Millennium Falcon");
        millenniumFalcon.Year.ShouldBe(2017);
        millenniumFalcon.ThemeId.ShouldBe(171);
        millenniumFalcon.NumParts.ShouldBe(7541);
    }

    [Fact]
    public async Task ImportSetsAsync_UpdatesExistingSetsAsync() {
        // Arrange
        await SeedThemesAsync();
        _context.RebrickableSets.Add(new RebrickableSet {
            SetNum = "75192-1",
            Name = "Old Name",
            Year = 2000,
            ThemeId = 1,
            NumParts = 100
        });
        await _context.SaveChangesAsync(TestCancellationToken);

        var csv = await Resources.GetStringAsync("sets.csv");
        _httpHandler.SetupZipResponse("sets.csv.zip", "sets.csv", csv);

        // Act
        await _sut.ImportSetsAsync(TestCancellationToken);

        // Assert
        var set = await _context.RebrickableSets.FindAsync(["75192-1"], TestCancellationToken);
        set!.Name.ShouldBe("Millennium Falcon");
        set.Year.ShouldBe(2017);
        set.NumParts.ShouldBe(7541);
    }

    // Minifig import tests

    [Fact]
    public async Task ImportMinifigsAsync_ParsesMinifigsCorrectlyAsync() {
        // Arrange
        var csv = await Resources.GetStringAsync("minifigs.csv");
        _httpHandler.SetupZipResponse("minifigs.csv.zip", "minifigs.csv", csv);

        // Act
        await _sut.ImportMinifigsAsync(TestCancellationToken);

        // Assert
        var minifigs = await _context.RebrickableMinifigs.ToListAsync(TestCancellationToken);
        minifigs.Count.ShouldBe(3);

        var chewbacca = minifigs.Single(m => m.FigNum == "fig-000001");
        chewbacca.Name.ShouldBe("Chewbacca");
        chewbacca.NumParts.ShouldBe(4);
    }

    [Fact]
    public async Task ImportMinifigsAsync_HandlesQuotedFieldsWithCommasAsync() {
        // Arrange
        var csv = await Resources.GetStringAsync("minifigs.csv");
        _httpHandler.SetupZipResponse("minifigs.csv.zip", "minifigs.csv", csv);

        // Act
        await _sut.ImportMinifigsAsync(TestCancellationToken);

        // Assert
        var c3po = await _context.RebrickableMinifigs.FindAsync(["fig-000003"], TestCancellationToken);
        c3po!.Name.ShouldBe("C-3PO, Colorful Wires");
    }

    [Fact]
    public async Task ImportMinifigsAsync_UpdatesExistingMinifigsAsync() {
        // Arrange
        _context.RebrickableMinifigs.Add(new RebrickableMinifig {
            FigNum = "fig-000001",
            Name = "Old Name",
            NumParts = 1
        });
        await _context.SaveChangesAsync(TestCancellationToken);

        var csv = await Resources.GetStringAsync("minifigs.csv");
        _httpHandler.SetupZipResponse("minifigs.csv.zip", "minifigs.csv", csv);

        // Act
        await _sut.ImportMinifigsAsync(TestCancellationToken);

        // Assert
        var minifig = await _context.RebrickableMinifigs.FindAsync(["fig-000001"], TestCancellationToken);
        minifig!.Name.ShouldBe("Chewbacca");
        minifig.NumParts.ShouldBe(4);
    }

    // Inventory import tests

    [Fact]
    public async Task ImportInventoriesAsync_ParsesInventoriesCorrectlyAsync() {
        // Arrange - need themes and sets first due to FK
        await SeedThemesAsync();
        await SeedSetsAsync();

        var csv = await Resources.GetStringAsync("inventories.csv");
        _httpHandler.SetupZipResponse("inventories.csv.zip", "inventories.csv", csv);

        // Act
        await _sut.ImportInventoriesAsync(TestCancellationToken);

        // Assert
        var inventories = await _context.RebrickableInventories.ToListAsync(TestCancellationToken);
        inventories.Count.ShouldBe(3);

        var inventory = inventories.Single(i => i.Id == 1);
        inventory.Version.ShouldBe(1);
        inventory.SetNum.ShouldBe("75192-1");

        var inventoryV2 = inventories.Single(i => i.Id == 3);
        inventoryV2.Version.ShouldBe(2);
    }

    [Fact]
    public async Task ImportInventoriesAsync_UpdatesExistingInventoriesAsync() {
        // Arrange
        await SeedThemesAsync();
        await SeedSetsAsync();

        _context.RebrickableInventories.Add(new RebrickableInventory {
            Id = 1,
            Version = 99,
            SetNum = "75192-1"
        });
        await _context.SaveChangesAsync(TestCancellationToken);

        var csv = await Resources.GetStringAsync("inventories.csv");
        _httpHandler.SetupZipResponse("inventories.csv.zip", "inventories.csv", csv);

        // Act
        await _sut.ImportInventoriesAsync(TestCancellationToken);

        // Assert
        var inventory = await _context.RebrickableInventories.FindAsync([1], TestCancellationToken);
        inventory!.Version.ShouldBe(1);
    }

    // Inventory set import tests

    [Fact]
    public async Task ImportInventorySetsAsync_ParsesInventorySetsCorrectlyAsync() {
        // Arrange
        await SeedThemesAsync();
        await SeedSetsAsync();
        await SeedInventoriesAsync();

        var csv = await Resources.GetStringAsync("inventory_sets.csv");
        _httpHandler.SetupZipResponse("inventory_sets.csv.zip", "inventory_sets.csv", csv);

        // Act
        await _sut.ImportInventorySetsAsync(TestCancellationToken);

        // Assert
        var inventorySets = await _context.RebrickableInventorySets.ToListAsync(TestCancellationToken);
        inventorySets.Count.ShouldBe(2);

        var inventorySet = inventorySets.Single(s => s.InventoryId == 1);
        inventorySet.SetNum.ShouldBe("75192-1");
        inventorySet.Quantity.ShouldBe(1);
    }

    [Fact]
    public async Task ImportInventorySetsAsync_UpdatesExistingInventorySetsAsync() {
        // Arrange
        await SeedThemesAsync();
        await SeedSetsAsync();
        await SeedInventoriesAsync();

        _context.RebrickableInventorySets.Add(new RebrickableInventorySet {
            InventoryId = 1,
            SetNum = "75192-1",
            Quantity = 99
        });
        await _context.SaveChangesAsync(TestCancellationToken);

        var csv = await Resources.GetStringAsync("inventory_sets.csv");
        _httpHandler.SetupZipResponse("inventory_sets.csv.zip", "inventory_sets.csv", csv);

        // Act
        await _sut.ImportInventorySetsAsync(TestCancellationToken);

        // Assert
        var inventorySet = await _context.RebrickableInventorySets.FindAsync([1, "75192-1"], TestCancellationToken);
        inventorySet!.Quantity.ShouldBe(1);
    }

    // Inventory minifig import tests

    [Fact]
    public async Task ImportInventoryMinifigsAsync_ParsesInventoryMinifigsCorrectlyAsync() {
        // Arrange
        await SeedThemesAsync();
        await SeedSetsAsync();
        await SeedInventoriesAsync();
        await SeedMinifigsAsync();

        var csv = await Resources.GetStringAsync("inventory_minifigs.csv");
        _httpHandler.SetupZipResponse("inventory_minifigs.csv.zip", "inventory_minifigs.csv", csv);

        // Act
        await _sut.ImportInventoryMinifigsAsync(TestCancellationToken);

        // Assert
        var inventoryMinifigs = await _context.RebrickableInventoryMinifigs.ToListAsync(TestCancellationToken);
        inventoryMinifigs.Count.ShouldBe(3);

        var inventoryMinifig = inventoryMinifigs.Single(m => m.InventoryId == 1 && m.FigNum == "fig-000001");
        inventoryMinifig.Quantity.ShouldBe(1);
    }

    [Fact]
    public async Task ImportInventoryMinifigsAsync_UpdatesExistingInventoryMinifigsAsync() {
        // Arrange
        await SeedThemesAsync();
        await SeedSetsAsync();
        await SeedInventoriesAsync();
        await SeedMinifigsAsync();

        _context.RebrickableInventoryMinifigs.Add(new RebrickableInventoryMinifig {
            InventoryId = 1,
            FigNum = "fig-000001",
            Quantity = 99
        });
        await _context.SaveChangesAsync(TestCancellationToken);

        var csv = await Resources.GetStringAsync("inventory_minifigs.csv");
        _httpHandler.SetupZipResponse("inventory_minifigs.csv.zip", "inventory_minifigs.csv", csv);

        // Act
        await _sut.ImportInventoryMinifigsAsync(TestCancellationToken);

        // Assert
        var inventoryMinifig = await _context.RebrickableInventoryMinifigs.FindAsync([1, "fig-000001"], TestCancellationToken);
        inventoryMinifig!.Quantity.ShouldBe(1);
    }

    // ImportAllAsync tests

    [Fact]
    public async Task ImportAllAsync_ImportsAllEntitiesInCorrectOrderAsync() {
        // Arrange
        _httpHandler.SetupZipResponse("themes.csv.zip", "themes.csv", await Resources.GetStringAsync("themes.csv"));
        _httpHandler.SetupZipResponse("sets.csv.zip", "sets.csv", await Resources.GetStringAsync("sets.csv"));
        _httpHandler.SetupZipResponse("minifigs.csv.zip", "minifigs.csv", await Resources.GetStringAsync("minifigs.csv"));
        _httpHandler.SetupZipResponse("inventories.csv.zip", "inventories.csv", await Resources.GetStringAsync("inventories.csv"));
        _httpHandler.SetupZipResponse("inventory_sets.csv.zip", "inventory_sets.csv", await Resources.GetStringAsync("inventory_sets.csv"));
        _httpHandler.SetupZipResponse("inventory_minifigs.csv.zip", "inventory_minifigs.csv", await Resources.GetStringAsync("inventory_minifigs.csv"));

        // Act
        await _sut.ImportAllAsync(TestCancellationToken);

        // Assert
        (await _context.RebrickableThemes.CountAsync(TestCancellationToken)).ShouldBe(4);
        (await _context.RebrickableSets.CountAsync(TestCancellationToken)).ShouldBe(4);
        (await _context.RebrickableMinifigs.CountAsync(TestCancellationToken)).ShouldBe(3);
        (await _context.RebrickableInventories.CountAsync(TestCancellationToken)).ShouldBe(3);
        (await _context.RebrickableInventorySets.CountAsync(TestCancellationToken)).ShouldBe(2);
        (await _context.RebrickableInventoryMinifigs.CountAsync(TestCancellationToken)).ShouldBe(3);
    }

    // Error handling tests

    [Fact]
    public async Task ImportThemesAsync_WhenDownloadFails_ThrowsExceptionAsync() {
        // Arrange
        _httpHandler.SetupErrorResponse("themes.csv.zip", HttpStatusCode.NotFound);

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.ImportThemesAsync(TestCancellationToken));
    }

    [Fact]
    public async Task ImportThemesAsync_WhenZipIsEmpty_ThrowsExceptionAsync() {
        // Arrange
        _httpHandler.SetupEmptyZipResponse("themes.csv.zip");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.ImportThemesAsync(TestCancellationToken));
    }

    // Helper methods

    private async Task SeedThemesAsync() {
        _context.RebrickableThemes.AddRange(
            new RebrickableTheme { Id = 1, Name = "Technic" },
            new RebrickableTheme { Id = 158, Name = "Star Wars" },
            new RebrickableTheme { Id = 171, Name = "Star Wars Ultimate Collector Series", ParentId = 158 },
            new RebrickableTheme { Id = 246, Name = "Architecture" }
        );
        await _context.SaveChangesAsync(TestCancellationToken);
    }

    private async Task SeedSetsAsync() {
        _context.RebrickableSets.AddRange(
            new RebrickableSet { SetNum = "75192-1", Name = "Millennium Falcon", Year = 2017, ThemeId = 171, NumParts = 7541 },
            new RebrickableSet { SetNum = "10179-1", Name = "Ultimate Collector's Millennium Falcon", Year = 2007, ThemeId = 171, NumParts = 5195 },
            new RebrickableSet { SetNum = "42056-1", Name = "Porsche 911 GT3 RS", Year = 2016, ThemeId = 1, NumParts = 2704 },
            new RebrickableSet { SetNum = "21054-1", Name = "The White House", Year = 2020, ThemeId = 246, NumParts = 1483 }
        );
        await _context.SaveChangesAsync(TestCancellationToken);
    }

    private async Task SeedMinifigsAsync() {
        _context.RebrickableMinifigs.AddRange(
            new RebrickableMinifig { FigNum = "fig-000001", Name = "Chewbacca", NumParts = 4 },
            new RebrickableMinifig { FigNum = "fig-000002", Name = "Han Solo", NumParts = 4 },
            new RebrickableMinifig { FigNum = "fig-000003", Name = "C-3PO, Colorful Wires", NumParts = 5 }
        );
        await _context.SaveChangesAsync(TestCancellationToken);
    }

    private async Task SeedInventoriesAsync() {
        _context.RebrickableInventories.AddRange(
            new RebrickableInventory { Id = 1, Version = 1, SetNum = "75192-1" },
            new RebrickableInventory { Id = 2, Version = 1, SetNum = "10179-1" },
            new RebrickableInventory { Id = 3, Version = 2, SetNum = "75192-1" }
        );
        await _context.SaveChangesAsync(TestCancellationToken);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler {
        private readonly Dictionary<string, Func<HttpResponseMessage>> _responseFactories = [];

        public void SetupZipResponse(string filename, string entryName, string content) {
            _responseFactories[$"https://cdn.rebrickable.com/media/downloads/{filename}"] = () => {
                var zipStream = CreateZipStream(entryName, content);
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StreamContent(zipStream)
                };
            };
        }

        public void SetupErrorResponse(string filename, HttpStatusCode statusCode) {
            _responseFactories[$"https://cdn.rebrickable.com/media/downloads/{filename}"] = () =>
                new HttpResponseMessage(statusCode);
        }

        public void SetupEmptyZipResponse(string filename) {
            _responseFactories[$"https://cdn.rebrickable.com/media/downloads/{filename}"] = () => {
                var ms = new MemoryStream();
                using(var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true)) {
                    // Empty archive
                }

                ms.Position = 0;
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StreamContent(ms)
                };
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            var url = request.RequestUri?.AbsoluteUri ?? string.Empty;

            if(_responseFactories.TryGetValue(url, out var factory)) {
                return Task.FromResult(factory());
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static MemoryStream CreateZipStream(string entryName, string content) {
            var ms = new MemoryStream();
            using(var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true)) {
                var entry = archive.CreateEntry(entryName);
                using var entryStream = entry.Open();
                var bytes = Encoding.UTF8.GetBytes(content);
                entryStream.Write(bytes, 0, bytes.Length);
            }

            ms.Position = 0;
            return ms;
        }
    }
}
