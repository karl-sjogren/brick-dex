using System.IO.Abstractions;
using BrickDex.Lucene.Options;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Directory = Lucene.Net.Store.Directory;

namespace BrickDex.Lucene.Index;

/// <summary>
/// Factory for creating Lucene directory instances based on configuration.
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
    /// Creates a Lucene directory based on the configured storage type.
    /// </summary>
    public Directory CreateDirectory() {
        return _options.StorageType switch {
            StorageType.Azure => CreateAzureDirectory(),
            _ => CreateFileSystemDirectory()
        };
    }

    private Directory CreateFileSystemDirectory() {
        var path = _options.LocalPath
            ?? _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "BrickDex", "LuceneIndex");

        _fileSystem.Directory.CreateDirectory(path);

        _logger.LogInformation("Using FileSystem directory for Lucene index at {Path}", path);

        return FSDirectory.Open(path);
    }

    private Directory CreateAzureDirectory() {
        if(string.IsNullOrEmpty(_options.AzureConnectionString)) {
            throw new InvalidOperationException(
                "Azure connection string is required when StorageType is Azure. " +
                "Set 'Lucene:AzureConnectionString' in configuration.");
        }

        _logger.LogInformation(
            "Using Azure directory for Lucene index in container {Container}",
            _options.AzureContainerName);

        return new AzureDirectory(_options.AzureConnectionString, _options.AzureContainerName);
    }
}
