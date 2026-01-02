using System.Net.Http.Json;
using BrickDex.Functions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrickDex.Functions.Services;

/// <summary>
/// Client for calling the Web app's reindex webhook.
/// </summary>
public interface IReindexWebhookClient {
    Task<bool> TriggerReindexAsync(CancellationToken cancellationToken = default);
}

public class ReindexWebhookClient : IReindexWebhookClient {
    private readonly HttpClient _httpClient;
    private readonly WebAppOptions _options;
    private readonly ILogger<ReindexWebhookClient> _logger;

    public ReindexWebhookClient(
        HttpClient httpClient,
        IOptions<WebAppOptions> options,
        ILogger<ReindexWebhookClient> logger) {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> TriggerReindexAsync(CancellationToken cancellationToken = default) {
        _logger.LogInformation("Triggering reindex webhook");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/reindex");
        request.Headers.Add("X-Api-Key", _options.ReindexApiKey);

        try {
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if(response.IsSuccessStatusCode) {
                var result = await response.Content.ReadFromJsonAsync<ReindexResponse>(cancellationToken);
                _logger.LogInformation("Reindex completed successfully in {DurationMs}ms", result?.DurationMs);
                return true;
            }

            _logger.LogError("Reindex webhook failed with status {StatusCode}: {ReasonPhrase}",
                response.StatusCode, response.ReasonPhrase);
            return false;
        } catch(Exception ex) {
            _logger.LogError(ex, "Failed to call reindex webhook");
            throw;
        }
    }

    private sealed record ReindexResponse(bool Success, string Message, long DurationMs);
}
