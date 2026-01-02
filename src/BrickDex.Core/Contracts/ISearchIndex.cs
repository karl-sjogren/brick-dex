namespace BrickDex.Core.Contracts;

/// <summary>
/// Manages the Lucene search index for sets, user sets, and minifigs.
/// </summary>
public interface ISearchIndex {
    /// <summary>
    /// Rebuilds the entire search index from the database.
    /// </summary>
    Task RebuildIndexAsync(CancellationToken cancellationToken = default);

    // Set operations
    Task IndexSetAsync(string setNum, CancellationToken cancellationToken = default);
    Task IndexSetsAsync(IEnumerable<string> setNums, CancellationToken cancellationToken = default);
    Task RemoveSetAsync(string setNum, CancellationToken cancellationToken = default);

    // User set operations
    Task IndexUserSetAsync(Guid userId, string setNum, CancellationToken cancellationToken = default);
    Task RemoveUserSetAsync(Guid userId, string setNum, CancellationToken cancellationToken = default);
    Task RemoveAllUserSetsAsync(Guid userId, CancellationToken cancellationToken = default);

    // Minifig operations
    Task IndexMinifigAsync(string figNum, CancellationToken cancellationToken = default);
    Task IndexMinifigsAsync(IEnumerable<string> figNums, CancellationToken cancellationToken = default);
    Task RemoveMinifigAsync(string figNum, CancellationToken cancellationToken = default);

    // Index management
    Task OptimizeAsync(CancellationToken cancellationToken = default);
    Task<IndexStats> GetStatsAsync(CancellationToken cancellationToken = default);
}

public record IndexStats(
    int TotalDocuments,
    int SetDocuments,
    int UserSetDocuments,
    int MinifigDocuments,
    DateTimeOffset? LastUpdated,
    long IndexSizeBytes
);
