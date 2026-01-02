using BrickDex.Core.Models;
using BrickDex.Core.Models.Rebrickable;
using BrickDex.Core.Models.Search;
using Lucene.Net.Documents;
using Lucene.Net.Index;

namespace BrickDex.Lucene.Documents;

/// <summary>
/// Maps between domain models and Lucene documents.
/// </summary>
public static class DocumentMapper {
    // Document type discriminators
    public const string DocTypeSet = "set";
    public const string DocTypeUserSet = "userset";
    public const string DocTypeMinifig = "minifig";

    // Field names
    public const string FieldDocId = "doc_id";
    public const string FieldDocType = "doc_type";
    public const string FieldSetNum = "set_num";
    public const string FieldFigNum = "fig_num";
    public const string FieldName = "name";
    public const string FieldNameSort = "name_sort";
    public const string FieldYear = "year";
    public const string FieldThemeId = "theme_id";
    public const string FieldThemeName = "theme_name";
    public const string FieldNumParts = "num_parts";
    public const string FieldImageUrl = "image_url";
    public const string FieldUserId = "user_id";
    public const string FieldUserSetId = "user_set_id";
    public const string FieldQuantity = "quantity";
    public const string FieldStatus = "status";
    public const string FieldIsWishlist = "is_wishlist";
    public const string FieldNotes = "notes";
    public const string FieldSearchText = "search_text";

    /// <summary>
    /// Creates a Lucene document from a RebrickableSet.
    /// </summary>
    public static Document ToDocument(RebrickableSet set) {
        var doc = new Document {
            new StringField(FieldDocId, set.SetNum, Field.Store.YES),
            new StringField(FieldDocType, DocTypeSet, Field.Store.YES),
            new StringField(FieldSetNum, set.SetNum, Field.Store.YES),
            new TextField(FieldName, set.Name, Field.Store.YES),
            new SortedDocValuesField(FieldNameSort, new global::Lucene.Net.Util.BytesRef(set.Name.ToLowerInvariant())),
            new Int32Field(FieldYear, set.Year, Field.Store.YES),
            new Int32Field(FieldThemeId, set.ThemeId, Field.Store.YES),
            new Int32Field(FieldNumParts, set.NumParts, Field.Store.YES),
            // NumericDocValues for sorting
            new NumericDocValuesField(FieldYear + "_sort", set.Year),
            new NumericDocValuesField(FieldNumParts + "_sort", set.NumParts)
        };

        // Optional fields
        if(set.ImageUrl != null) {
            doc.Add(new StoredField(FieldImageUrl, set.ImageUrl));
        }

        if(set.Theme != null) {
            doc.Add(new TextField(FieldThemeName, set.Theme.Name, Field.Store.YES));
        }

        // Combined search text
        var searchText = BuildSearchText(set.SetNum, set.Name, set.Theme?.Name);
        doc.Add(new TextField(FieldSearchText, searchText, Field.Store.NO));

        return doc;
    }

    /// <summary>
    /// Creates a Lucene document from a UserSet.
    /// </summary>
    public static Document ToDocument(UserSet userSet) {
        var docId = $"{userSet.UserId}:{userSet.SetNumber}";

        var doc = new Document {
            new StringField(FieldDocId, docId, Field.Store.YES),
            new StringField(FieldDocType, DocTypeUserSet, Field.Store.YES),
            new StringField(FieldSetNum, userSet.SetNumber, Field.Store.YES),
            new StringField(FieldUserId, userSet.UserId.ToString(), Field.Store.YES),
            new StringField(FieldUserSetId, userSet.Id.ToString(), Field.Store.YES),
            new Int32Field(FieldQuantity, userSet.Quantity, Field.Store.YES),
            new Int32Field(FieldStatus, (int)userSet.Status, Field.Store.YES),
            new StringField(FieldIsWishlist, userSet.IsWishlist.ToString().ToLowerInvariant(), Field.Store.YES)
        };

        // Fields from the related Set
        if(userSet.Set != null) {
            doc.Add(new TextField(FieldName, userSet.Set.Name, Field.Store.YES));
            doc.Add(new SortedDocValuesField(FieldNameSort, new global::Lucene.Net.Util.BytesRef(userSet.Set.Name.ToLowerInvariant())));
            doc.Add(new Int32Field(FieldYear, userSet.Set.Year, Field.Store.YES));
            doc.Add(new Int32Field(FieldThemeId, userSet.Set.ThemeId, Field.Store.YES));
            doc.Add(new Int32Field(FieldNumParts, userSet.Set.NumParts, Field.Store.YES));
            doc.Add(new NumericDocValuesField(FieldYear + "_sort", userSet.Set.Year));
            doc.Add(new NumericDocValuesField(FieldNumParts + "_sort", userSet.Set.NumParts));

            if(userSet.Set.ImageUrl != null) {
                doc.Add(new StoredField(FieldImageUrl, userSet.Set.ImageUrl));
            }

            if(userSet.Set.Theme != null) {
                doc.Add(new TextField(FieldThemeName, userSet.Set.Theme.Name, Field.Store.YES));
            }

            // Combined search text
            var searchText = BuildSearchText(userSet.SetNumber, userSet.Set.Name, userSet.Set.Theme?.Name, userSet.Notes);
            doc.Add(new TextField(FieldSearchText, searchText, Field.Store.NO));
        }

        // Optional notes
        if(!string.IsNullOrWhiteSpace(userSet.Notes)) {
            doc.Add(new TextField(FieldNotes, userSet.Notes, Field.Store.YES));
        }

        return doc;
    }

    /// <summary>
    /// Creates a Lucene document from a RebrickableMinifig.
    /// </summary>
    public static Document ToDocument(RebrickableMinifig minifig) {
        var docId = $"minifig:{minifig.FigNum}";

        var doc = new Document {
            new StringField(FieldDocId, docId, Field.Store.YES),
            new StringField(FieldDocType, DocTypeMinifig, Field.Store.YES),
            new StringField(FieldFigNum, minifig.FigNum, Field.Store.YES),
            new TextField(FieldName, minifig.Name, Field.Store.YES),
            new SortedDocValuesField(FieldNameSort, new global::Lucene.Net.Util.BytesRef(minifig.Name.ToLowerInvariant())),
            new Int32Field(FieldNumParts, minifig.NumParts, Field.Store.YES),
            new NumericDocValuesField(FieldNumParts + "_sort", minifig.NumParts)
        };

        if(minifig.ImageUrl != null) {
            doc.Add(new StoredField(FieldImageUrl, minifig.ImageUrl));
        }

        // Combined search text
        var searchText = BuildSearchText(minifig.FigNum, minifig.Name);
        doc.Add(new TextField(FieldSearchText, searchText, Field.Store.NO));

        return doc;
    }

    /// <summary>
    /// Maps a Lucene document to a SetSearchHit.
    /// </summary>
    public static SetSearchHit ToSetSearchHit(Document doc, float score) {
        return new SetSearchHit(
            SetNum: doc.Get(FieldSetNum) ?? "",
            Name: doc.Get(FieldName) ?? "",
            Year: doc.GetField(FieldYear)?.GetInt32Value() ?? 0,
            ThemeId: doc.GetField(FieldThemeId)?.GetInt32Value() ?? 0,
            ThemeName: doc.Get(FieldThemeName),
            NumParts: doc.GetField(FieldNumParts)?.GetInt32Value() ?? 0,
            ImageUrl: doc.Get(FieldImageUrl),
            Score: score
        );
    }

    /// <summary>
    /// Maps a Lucene document to a UserSetSearchHit.
    /// </summary>
    public static UserSetSearchHit ToUserSetSearchHit(Document doc, float score) {
        var userSetIdStr = doc.Get(FieldUserSetId);
        var userSetId = Guid.TryParse(userSetIdStr, out var id) ? id : Guid.Empty;

        return new UserSetSearchHit(
            UserSetId: userSetId,
            SetNum: doc.Get(FieldSetNum) ?? "",
            Name: doc.Get(FieldName) ?? "",
            Year: doc.GetField(FieldYear)?.GetInt32Value() ?? 0,
            ThemeId: doc.GetField(FieldThemeId)?.GetInt32Value() ?? 0,
            ThemeName: doc.Get(FieldThemeName),
            NumParts: doc.GetField(FieldNumParts)?.GetInt32Value() ?? 0,
            ImageUrl: doc.Get(FieldImageUrl),
            Quantity: doc.GetField(FieldQuantity)?.GetInt32Value() ?? 1,
            Status: (SetStatus)(doc.GetField(FieldStatus)?.GetInt32Value() ?? 0),
            IsWishlist: doc.Get(FieldIsWishlist)?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false,
            Notes: doc.Get(FieldNotes),
            Score: score
        );
    }

    /// <summary>
    /// Maps a Lucene document to a MinifigSearchHit.
    /// </summary>
    public static MinifigSearchHit ToMinifigSearchHit(Document doc, float score) {
        return new MinifigSearchHit(
            FigNum: doc.Get(FieldFigNum) ?? "",
            Name: doc.Get(FieldName) ?? "",
            NumParts: doc.GetField(FieldNumParts)?.GetInt32Value() ?? 0,
            ImageUrl: doc.Get(FieldImageUrl),
            Score: score
        );
    }

    /// <summary>
    /// Creates a term for deleting a document by ID.
    /// </summary>
    public static Term CreateDeleteTerm(string docId) {
        return new Term(FieldDocId, docId);
    }

    private static string BuildSearchText(params string?[] parts) {
        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
