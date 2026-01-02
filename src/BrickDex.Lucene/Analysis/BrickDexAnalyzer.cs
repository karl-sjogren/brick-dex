using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace BrickDex.Lucene.Analysis;

/// <summary>
/// Custom analyzer for BrickDex search with stemming and normalization.
/// </summary>
public class BrickDexAnalyzer : Analyzer {
    private readonly LuceneVersion _version;

    public BrickDexAnalyzer(LuceneVersion version) {
        _version = version;
    }

    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader) {
        // Standard tokenizer handles most text well
        var tokenizer = new StandardTokenizer(_version, reader);

        TokenStream stream = tokenizer;

        // Lowercase for case-insensitive search
        stream = new LowerCaseFilter(_version, stream);

        // Handle ASCII folding (accents, special chars)
        stream = new ASCIIFoldingFilter(stream);

        // English possessive filter ("Luke's" -> "Luke")
        stream = new EnglishPossessiveFilter(_version, stream);

        // Porter stemmer for English
        stream = new PorterStemFilter(stream);

        // Remove common stop words
        stream = new StopFilter(_version, stream, EnglishAnalyzer.DefaultStopSet);

        return new TokenStreamComponents(tokenizer, stream);
    }
}
