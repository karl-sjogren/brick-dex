using System.Text.Json.Serialization;

namespace BrickDex.Core.Services.Rebrickable;

public class RebrickableSearchResult<T> {
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("previous")]
    public string? Previous { get; set; }

    [JsonPropertyName("results")]
    public IReadOnlyList<T> Results { get; set; } = [];
}

public class RebrickableSet {
    [JsonPropertyName("set_num")]
    public string SetNumber { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("theme_id")]
    public int ThemeId { get; set; }

    [JsonPropertyName("num_parts")]
    public int NumParts { get; set; }

    [JsonPropertyName("set_img_url")]
    public string? SetImageUrl { get; set; }

    [JsonPropertyName("set_url")]
    public string? SetUrl { get; set; }

    [JsonPropertyName("last_modified_dt")]
    public string? LastModified { get; set; }
}

public class RebrickableTheme {
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("parent_id")]
    public int? ParentId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
