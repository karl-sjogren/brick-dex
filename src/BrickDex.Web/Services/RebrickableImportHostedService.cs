using BrickDex.Core.Contracts;

namespace BrickDex.Web.Services;

public class RebrickableImportHostedService : BackgroundService {
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RebrickableImportHostedService> _logger;

    public RebrickableImportHostedService(
            IServiceProvider serviceProvider,
            TimeProvider timeProvider,
            ILogger<RebrickableImportHostedService> logger) {
        _serviceProvider = serviceProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while(!stoppingToken.IsCancellationRequested) {
            var nextRun = GetTimeUntilNextRun();

            try {
                await ImportCatalogAsync(stoppingToken);
            } catch(Exception ex) {
                _logger.LogError(ex, "Error occurred during Rebrickable import. Retrying in 1 hour.");

                nextRun = TimeSpan.FromHours(1);
            }

            // Wait for the calculated time until the next run
            await Task.Delay(nextRun, stoppingToken);
        }
    }

    private async Task ImportCatalogAsync(CancellationToken stoppingToken) {
        var importService = _serviceProvider.GetRequiredService<IRebrickableCatalogImportService>();
        var searchIndex = _serviceProvider.GetRequiredService<ISearchIndex>();

        _logger.LogInformation("Starting Rebrickable catalog import at {Time}", _timeProvider.GetUtcNow());
        await importService.ImportAllAsync(stoppingToken);
        _logger.LogInformation("Rebrickable catalog import completed successfully");

        _logger.LogInformation("Triggering search index rebuild...");
        await searchIndex.RebuildIndexAsync(stoppingToken);
        _logger.LogInformation("Search index rebuild completed successfully");
    }

    private TimeSpan GetTimeUntilNextRun() {
        var now = _timeProvider.GetUtcNow();
        var nextRun = new DateTime(now.Year, now.Month, now.Day, 3, 0, 0, DateTimeKind.Utc);

        // If it's already past 3am UTC today, schedule for next Sunday
        if(now >= nextRun) {
            nextRun = nextRun.AddDays(7);
        }

        // Adjust to the next Sunday
        while(nextRun.DayOfWeek != DayOfWeek.Sunday) {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun - now;
    }
}
