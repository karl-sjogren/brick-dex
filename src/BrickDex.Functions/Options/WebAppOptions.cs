using System.ComponentModel.DataAnnotations;

namespace BrickDex.Functions.Options;

/// <summary>
/// Configuration options for calling the Web app API.
/// </summary>
public class WebAppOptions {
    public const string SectionName = "WebApp";

    /// <summary>
    /// Base URL of the Web app (e.g., https://localhost:5001 or https://brickdex.azurewebsites.net).
    /// </summary>
    [Required]
    public required string BaseUrl { get; set; }

    /// <summary>
    /// API key for authenticating with the Web app's webhook endpoints.
    /// </summary>
    [Required]
    public required string ReindexApiKey { get; set; }
}
