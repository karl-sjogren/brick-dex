using System.IO.Abstractions;
using BrickDex.Lucene.Options;
using Lucene.Net.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Directory = Lucene.Net.Store.Directory;

namespace BrickDex.Lucene.Index;

/// <summary>
/// Factory for creating Lucene directory instances.
/// </summary>
public class IndexStorageFactory {
    private readonly LuceneOptions _options;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<IndexStorageFactory> _logger;

    public IndexStorageFactory(
        IOptions<LuceneOptions> options,
        IFileSystem fileSystem,
        ILogger<IndexStorageFactory> logger) {
        _options = options.Value;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Creates a Lucene directory for the file system.
    /// </summary>
    public Directory CreateDirectory() {
        var path = _options.LocalPath
            ?? _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "BrickDex", "LuceneIndex");

        _fileSystem.Directory.CreateDirectory(path);

        _logger.LogInformation("Using FileSystem directory for Lucene index at {Path}", path);

        return FSDirectory.Open(path);
    }
}
