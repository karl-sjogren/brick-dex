namespace BrickDex.Lucene.Options;

/// <summary>
/// Configuration options for Lucene search index.
/// </summary>
public class LuceneOptions {
    public const string SectionName = "Lucene";

    /// <summary>
    /// Local file system path for the index.
    /// </summary>
    public string? LocalPath { get; set; }

    /// <summary>
    /// Number of documents to batch before committing to the index.
    /// </summary>
    public int CommitBatchSize { get; set; } = 1000;

    /// <summary>
    /// RAM buffer size in MB for index writer.
    /// </summary>
    public double RamBufferSizeMB { get; set; } = 256;
}
