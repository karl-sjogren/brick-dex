using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Lucene.Analysis;
using BrickDex.Lucene.Documents;
using BrickDex.Lucene.Options;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Directory = Lucene.Net.Store.Directory;

namespace BrickDex.Lucene.Index;

/// <summary>
/// Lucene-based search index implementation.
/// </summary>
public class LuceneSearchIndex : ISearchIndex, IDisposable {
    private const LuceneVersion _luceneVersion = LuceneVersion.LUCENE_48;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IndexStorageFactory _storageFactory;
    private readonly LuceneOptions _options;
    private readonly ILogger<LuceneSearchIndex> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private Directory? _directory;
    private IndexWriter? _writer;
    private DateTimeOffset? _lastUpdated;

    public LuceneSearchIndex(
        IServiceScopeFactory scopeFactory,
        IndexStorageFactory storageFactory,
        IOptions<LuceneOptions> options,
        ILogger<LuceneSearchIndex> logger,
        TimeProvider timeProvider) {
        _scopeFactory = scopeFactory;
        _storageFactory = storageFactory;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task RebuildIndexAsync(CancellationToken cancellationToken = default) {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            _logger.LogInformation("Starting full index rebuild");

            var writer = GetOrCreateWriter(create: true);

            // Delete all existing documents
            writer.DeleteAll();

            // Index all sets
            var setCount = await IndexAllSetsAsync(writer, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Indexed {SetCount} sets", setCount);

            // Index all user sets
            var userSetCount = await IndexAllUserSetsAsync(writer, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Indexed {UserSetCount} user sets", userSetCount);

            // Index all minifigs
            var minifigCount = await IndexAllMinifigsAsync(writer, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Indexed {MinifigCount} minifigs", minifigCount);

            writer.Commit();
            _lastUpdated = _timeProvider.GetUtcNow();

            _logger.LogInformation(
                "Index rebuild complete. Total documents: {Total}",
                setCount + userSetCount + minifigCount);
        } finally {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task IndexSetAsync(string setNum, CancellationToken cancellationToken = default) {
        await IndexSetsAsync([setNum], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task IndexSetsAsync(IEnumerable<string> setNums, CancellationToken cancellationToken = default) {
        var setNumList = setNums.ToList();
        if(setNumList.Count == 0) {
            return;
        }

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            var writer = GetOrCreateWriter();

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBrickDexContext>();

            var sets = await context.RebrickableSets
                .AsNoTracking()
                .Include(s => s.Theme)
                .Where(s => setNumList.Contains(s.SetNum))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach(var set in sets) {
                // Delete existing document
                writer.DeleteDocuments(DocumentMapper.CreateDeleteTerm(set.SetNum));

                // Add new document
                var doc = DocumentMapper.ToDocument(set);
                writer.AddDocument(doc);
            }

            writer.Commit();
            _lastUpdated = _timeProvider.GetUtcNow();

            _logger.LogDebug("Indexed {Count} sets", sets.Count);
        } finally {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveSetAsync(string setNum, CancellationToken cancellationToken = default) {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            var writer = GetOrCreateWriter();
            writer.DeleteDocuments(DocumentMapper.CreateDeleteTerm(setNum));
            writer.Commit();
            _lastUpdated = _timeProvider.GetUtcNow();
        } finally {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task IndexUserSetAsync(Guid userId, string setNum, CancellationToken cancellationToken = default) {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            var writer = GetOrCreateWriter();

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBrickDexContext>();

            var userSet = await context.UserSets
                .AsNoTracking()
                .Include(us => us.Set)
                    .ThenInclude(s => s.Theme)
                .FirstOrDefaultAsync(
                    us => us.UserId == userId && us.SetNumber == setNum,
                    cancellationToken)
                .ConfigureAwait(false);

            if(userSet == null) {
                _logger.LogWarning(
                    "UserSet not found for user {UserId} and set {SetNum}",
                    userId, setNum);
                return;
            }

            var docId = $"{userId}:{setNum}";

            // Delete existing document
            writer.DeleteDocuments(DocumentMapper.CreateDeleteTerm(docId));

            // Add new document
            var doc = DocumentMapper.ToDocument(userSet);
            writer.AddDocument(doc);

            writer.Commit();
            _lastUpdated = _timeProvider.GetUtcNow();

            _logger.LogDebug("Indexed user set {DocId}", docId);
        } finally {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveUserSetAsync(Guid userId, string setNum, CancellationToken cancellationToken = default) {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            var writer = GetOrCreateWriter();
            var docId = $"{userId}:{setNum}";
            writer.DeleteDocuments(DocumentMapper.CreateDeleteTerm(docId));
            writer.Commit();
            _lastUpdated = _timeProvider.GetUtcNow();

            _logger.LogDebug("Removed user set {DocId} from index", docId);
        } finally {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveAllUserSetsAsync(Guid userId, CancellationToken cancellationToken = default) {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            var writer = GetOrCreateWriter();

            // Delete all documents matching user_id
            var query = new global::Lucene.Net.Search.TermQuery(
                new global::Lucene.Net.Index.Term(DocumentMapper.FieldUserId, userId.ToString()));
            writer.DeleteDocuments(query);
            writer.Commit();
            _lastUpdated = _timeProvider.GetUtcNow();

            _logger.LogInformation("Removed all user sets for user {UserId} from index", userId);
        } finally {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task IndexMinifigAsync(string figNum, CancellationToken cancellationToken = default) {
        await IndexMinifigsAsync([figNum], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task IndexMinifigsAsync(IEnumerable<string> figNums, CancellationToken cancellationToken = default) {
        var figNumList = figNums.ToList();
        if(figNumList.Count == 0) {
            return;
        }

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            var writer = GetOrCreateWriter();

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBrickDexContext>();

            var minifigs = await context.RebrickableMinifigs
                .AsNoTracking()
                .Where(m => figNumList.Contains(m.FigNum))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach(var minifig in minifigs) {
                var docId = $"minifig:{minifig.FigNum}";

                // Delete existing document
                writer.DeleteDocuments(DocumentMapper.CreateDeleteTerm(docId));

                // Add new document
                var doc = DocumentMapper.ToDocument(minifig);
                writer.AddDocument(doc);
            }

            writer.Commit();
            _lastUpdated = _timeProvider.GetUtcNow();

            _logger.LogDebug("Indexed {Count} minifigs", minifigs.Count);
        } finally {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveMinifigAsync(string figNum, CancellationToken cancellationToken = default) {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            var writer = GetOrCreateWriter();
            var docId = $"minifig:{figNum}";
            writer.DeleteDocuments(DocumentMapper.CreateDeleteTerm(docId));
            writer.Commit();
            _lastUpdated = _timeProvider.GetUtcNow();
        } finally {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task OptimizeAsync(CancellationToken cancellationToken = default) {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            var writer = GetOrCreateWriter();
            writer.ForceMerge(1);
            writer.Commit();
            _logger.LogInformation("Index optimized");
        } finally {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public Task<IndexStats> GetStatsAsync(CancellationToken cancellationToken = default) {
        var directory = GetOrCreateDirectory();

        using var reader = DirectoryReader.Open(directory);
        var totalDocs = reader.NumDocs;

        // Count by doc type
        var setCount = 0;
        var userSetCount = 0;
        var minifigCount = 0;

        // Use a more efficient approach - just count via NumDocs
        // Iterating all docs is expensive, so we'll estimate based on total
        for(var i = 0; i < reader.MaxDoc; i++) {
            var doc = reader.Document(i, new HashSet<string> { DocumentMapper.FieldDocType });
            var docType = doc.Get(DocumentMapper.FieldDocType);

            switch(docType) {
                case DocumentMapper.DocTypeSet:
                    setCount++;
                    break;
                case DocumentMapper.DocTypeUserSet:
                    userSetCount++;
                    break;
                case DocumentMapper.DocTypeMinifig:
                    minifigCount++;
                    break;
            }
        }

        // Estimate index size (this is an approximation)
        long indexSize = 0;
        foreach(var file in directory.ListAll()) {
            indexSize += directory.FileLength(file);
        }

        var stats = new IndexStats(
            TotalDocuments: totalDocs,
            SetDocuments: setCount,
            UserSetDocuments: userSetCount,
            MinifigDocuments: minifigCount,
            LastUpdated: _lastUpdated,
            IndexSizeBytes: indexSize
        );

        return Task.FromResult(stats);
    }

    /// <summary>
    /// Gets the underlying directory for search operations.
    /// </summary>
    internal Directory GetOrCreateDirectory() {
        return _directory ??= _storageFactory.CreateDirectory();
    }

    private IndexWriter GetOrCreateWriter(bool create = false) {
        if(_writer != null && !create) {
            return _writer;
        }

        _writer?.Dispose();

        var directory = GetOrCreateDirectory();
        var analyzer = new BrickDexAnalyzer(_luceneVersion);
        var config = new IndexWriterConfig(_luceneVersion, analyzer) {
            OpenMode = create ? OpenMode.CREATE : OpenMode.CREATE_OR_APPEND,
            RAMBufferSizeMB = _options.RamBufferSizeMB
        };

        _writer = new IndexWriter(directory, config);
        return _writer;
    }

    private async Task<int> IndexAllSetsAsync(IndexWriter writer, CancellationToken cancellationToken) {
        var count = 0;
        var batchSize = _options.CommitBatchSize;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IBrickDexContext>();

        var sets = context.RebrickableSets
            .AsNoTracking()
            .Include(s => s.Theme)
            .AsAsyncEnumerable();

        await foreach(var set in sets.WithCancellation(cancellationToken).ConfigureAwait(false)) {
            var doc = DocumentMapper.ToDocument(set);
            writer.AddDocument(doc);
            count++;

            if(count % batchSize == 0) {
                _logger.LogDebug("Indexed {Count} sets...", count);
            }
        }

        return count;
    }

    private async Task<int> IndexAllUserSetsAsync(IndexWriter writer, CancellationToken cancellationToken) {
        var count = 0;
        var batchSize = _options.CommitBatchSize;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IBrickDexContext>();

        var userSets = context.UserSets
            .AsNoTracking()
            .Include(us => us.Set)
                .ThenInclude(s => s.Theme)
            .AsAsyncEnumerable();

        await foreach(var userSet in userSets.WithCancellation(cancellationToken).ConfigureAwait(false)) {
            var doc = DocumentMapper.ToDocument(userSet);
            writer.AddDocument(doc);
            count++;

            if(count % batchSize == 0) {
                _logger.LogDebug("Indexed {Count} user sets...", count);
            }
        }

        return count;
    }

    private async Task<int> IndexAllMinifigsAsync(IndexWriter writer, CancellationToken cancellationToken) {
        var count = 0;
        var batchSize = _options.CommitBatchSize;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IBrickDexContext>();

        var minifigs = context.RebrickableMinifigs
            .AsNoTracking()
            .AsAsyncEnumerable();

        await foreach(var minifig in minifigs.WithCancellation(cancellationToken).ConfigureAwait(false)) {
            var doc = DocumentMapper.ToDocument(minifig);
            writer.AddDocument(doc);
            count++;

            if(count % batchSize == 0) {
                _logger.LogDebug("Indexed {Count} minifigs...", count);
            }
        }

        return count;
    }

    public void Dispose() {
        _writer?.Dispose();
        _directory?.Dispose();
        _writeLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
