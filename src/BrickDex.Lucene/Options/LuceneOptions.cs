namespace BrickDex.Lucene.Options;

/// <summary>
/// Configuration options for Lucene search index.
/// </summary>
public class LuceneOptions {
    public const string SectionName = "Lucene";

    /// <summary>
    /// Storage type for the Lucene index.
    /// </summary>
    public StorageType StorageType { get; set; } = StorageType.FileSystem;

    /// <summary>
    /// Local file system path for the index (used when StorageType is FileSystem).
    /// </summary>
    public string? LocalPath { get; set; }

    /// <summary>
    /// Azure Storage connection string (used when StorageType is Azure).
    /// </summary>
    public string? AzureConnectionString { get; set; }

    /// <summary>
    /// Azure Storage container name for the index.
    /// </summary>
    public string AzureContainerName { get; set; } = "brickdex-lucene-index";

    /// <summary>
    /// Number of documents to batch before committing to the index.
    /// </summary>
    public int CommitBatchSize { get; set; } = 1000;

    /// <summary>
    /// RAM buffer size in MB for index writer.
    /// </summary>
    public double RamBufferSizeMB { get; set; } = 256;
}

/// <summary>
/// Storage type for the Lucene index.
/// </summary>
public enum StorageType {
    /// <summary>
    /// Store index on local file system.
    /// </summary>
    FileSystem,

    /// <summary>
    /// Store index in Azure Blob Storage.
    /// </summary>
    Azure
}
