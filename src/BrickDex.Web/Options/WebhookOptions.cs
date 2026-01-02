using System.ComponentModel.DataAnnotations;

namespace BrickDex.Web.Options;

/// <summary>
/// Configuration options for webhook endpoints.
/// </summary>
public class WebhookOptions {
    public const string SectionName = "Webhooks";

    /// <summary>
    /// API key required for authenticating webhook requests.
    /// </summary>
    [Required]
    public required string ReindexApiKey { get; set; }
}
