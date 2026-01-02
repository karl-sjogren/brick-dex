using BrickDex.Core.Contracts;
using BrickDex.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BrickDex.Functions;

public class RebrickableImportFunction {
    private readonly IRebrickableCatalogImportService _importService;
    private readonly IReindexWebhookClient _reindexClient;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RebrickableImportFunction> _logger;

    public RebrickableImportFunction(
        IRebrickableCatalogImportService importService,
        IReindexWebhookClient reindexClient,
        TimeProvider timeProvider,
        ILogger<RebrickableImportFunction> logger) {
        _importService = importService;
        _reindexClient = reindexClient;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    [Function("RebrickableImport")]
    public async Task RunAsync(
        [TimerTrigger("0 0 3 * * 0")] TimerInfo timerInfo,
        CancellationToken cancellationToken) {
        _logger.LogInformation("Starting weekly Rebrickable catalog import at {Time}", _timeProvider.GetUtcNow());

        try {
            await _importService.ImportAllAsync(cancellationToken);
            _logger.LogInformation("Rebrickable catalog import completed successfully");

            // Trigger reindex on the Web app
            _logger.LogInformation("Triggering search index rebuild via webhook...");
            var reindexSuccess = await _reindexClient.TriggerReindexAsync(cancellationToken);

            if(reindexSuccess) {
                _logger.LogInformation("Search index rebuild completed successfully");
            } else {
                _logger.LogWarning("Search index rebuild returned failure status");
            }
        } catch(Exception ex) {
            _logger.LogError(ex, "Rebrickable catalog import or reindex failed");
            throw;
        }

        if(timerInfo.ScheduleStatus is not null) {
            _logger.LogInformation("Next scheduled import: {NextRun}", timerInfo.ScheduleStatus.Next);
        }
    }
}
