using BrickDex.Core.Contracts;
using BrickDex.Core.Services.Rebrickable;
using BrickDex.Web.Options;
using Microsoft.Extensions.Options;

namespace BrickDex.Web.Services;

public class RebrickableClient : IRebrickableClient {
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<RebrickableClient> _logger;

    public RebrickableClient(HttpClient httpClient, IOptions<RebrickableOptions> options, ILogger<RebrickableClient> logger) {
        _httpClient = httpClient;
        _apiKey = options.Value.ApiKey;
        _logger = logger;
    }

    public async Task<RebrickableSet?> GetSetAsync(string setNumber, CancellationToken cancellationToken = default) {
        try {
            // Rebrickable expects set numbers to end with -1 for the main set
            var normalizedSetNumber = NormalizeSetNumber(setNumber);
            var response = await _httpClient.GetAsync($"lego/sets/{normalizedSetNumber}/?key={_apiKey}", cancellationToken);

            if(!response.IsSuccessStatusCode) {
                _logger.LogWarning("Failed to get set {SetNumber}: {StatusCode}", setNumber, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<RebrickableSet>(cancellationToken);
        } catch(Exception ex) {
            _logger.LogError(ex, "Error getting set {SetNumber}", setNumber);
            return null;
        }
    }

    public async Task<RebrickableSearchResult<RebrickableSet>> SearchSetsAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) {
        try {
            var response = await _httpClient.GetAsync(
                $"lego/sets/?key={_apiKey}&search={Uri.EscapeDataString(query)}&page={page}&page_size={pageSize}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<RebrickableSearchResult<RebrickableSet>>(cancellationToken)
                ?? new RebrickableSearchResult<RebrickableSet>();
        } catch(Exception ex) {
            _logger.LogError(ex, "Error searching sets with query {Query}", query);
            return new RebrickableSearchResult<RebrickableSet>();
        }
    }

    public async Task<RebrickableSearchResult<RebrickableTheme>> GetThemesAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default) {
        try {
            var response = await _httpClient.GetAsync(
                $"lego/themes/?key={_apiKey}&page={page}&page_size={pageSize}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<RebrickableSearchResult<RebrickableTheme>>(cancellationToken)
                ?? new RebrickableSearchResult<RebrickableTheme>();
        } catch(Exception ex) {
            _logger.LogError(ex, "Error getting themes");
            return new RebrickableSearchResult<RebrickableTheme>();
        }
    }

    private static string NormalizeSetNumber(string setNumber) {
        // If the set number doesn't contain a dash, add -1
        if(!setNumber.Contains('-', StringComparison.Ordinal)) {
            return $"{setNumber}-1";
        }

        return setNumber;
    }
}
