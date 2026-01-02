using BrickDex.Core.Contracts;
using Lucene.Net.Index;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BrickDex.Lucene.Services;

/// <summary>
/// Hosted service that ensures the search index is populated on startup.
/// </summary>
public class IndexInitializationService : IHostedService {
    private readonly ISearchIndex _searchIndex;
    private readonly ILogger<IndexInitializationService> _logger;

    public IndexInitializationService(
        ISearchIndex searchIndex,
        ILogger<IndexInitializationService> logger) {
        _searchIndex = searchIndex;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Checking search index status...");

        try {
            IndexStats? stats = null;
            try {
                stats = await _searchIndex.GetStatsAsync(cancellationToken);
            } catch(IndexNotFoundException ex) {
                _logger.LogWarning(ex, "Failed to get search index stats, assuming index is missing");
            }

            if(stats is null || stats.TotalDocuments == 0) {
                _logger.LogInformation("Search index is empty, starting rebuild...");
                await _searchIndex.RebuildIndexAsync(cancellationToken);
                _logger.LogInformation("Search index rebuild complete");
            } else {
                _logger.LogInformation(
                    "Search index contains {TotalDocuments} documents ({Sets} sets, {UserSets} user sets, {Minifigs} minifigs)",
                    stats.TotalDocuments,
                    stats.SetDocuments,
                    stats.UserSetDocuments,
                    stats.MinifigDocuments);
            }
        } catch(Exception ex) {
            _logger.LogError(ex, "Failed to initialize search index");
            // Don't rethrow - allow app to start even if index initialization fails
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
