using System.Net;
using System.Text.Json;
using BrickDex.Core.Options;
using BrickDex.Core.Services;
using BrickDex.Core.Services.Rebrickable;

namespace BrickDex.Core.Tests.Services;

public class RebrickableClientTests {
    private const string _testApiKey = "test-api-key";

    private readonly FakeHttpMessageHandler _httpHandler;
    private readonly RebrickableClient _sut;

    public RebrickableClientTests() {
        _httpHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(_httpHandler) {
            BaseAddress = new Uri("https://rebrickable.com/api/v3/")
        };

        var options = Microsoft.Extensions.Options.Options.Create(new RebrickableOptions { ApiKey = _testApiKey });
        var logger = new NullLogger<RebrickableClient>();

        _sut = new RebrickableClient(httpClient, options, logger);
    }

    [Fact]
    public async Task GetSetAsync_WhenSetExists_ReturnsSetAsync() {
        // Arrange
        var expectedSet = new RebrickableSet {
            SetNumber = "75192-1",
            Name = "Millennium Falcon",
            Year = 2017,
            NumParts = 7541,
            ThemeId = 158
        };

        _httpHandler.SetupResponse(
            $"https://rebrickable.com/api/v3/lego/sets/75192-1/?key={_testApiKey}",
            JsonSerializer.Serialize(expectedSet));

        // Act
        var result = await _sut.GetSetAsync("75192", TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.SetNumber.ShouldBe("75192-1");
        result.Name.ShouldBe("Millennium Falcon");
        result.Year.ShouldBe(2017);
        result.NumParts.ShouldBe(7541);
    }

    [Fact]
    public async Task GetSetAsync_WhenSetNumberAlreadyHasSuffix_DoesNotAddSuffixAsync() {
        // Arrange
        var expectedSet = new RebrickableSet {
            SetNumber = "75192-1",
            Name = "Millennium Falcon"
        };

        _httpHandler.SetupResponse(
            $"https://rebrickable.com/api/v3/lego/sets/75192-1/?key={_testApiKey}",
            JsonSerializer.Serialize(expectedSet));

        // Act
        var result = await _sut.GetSetAsync("75192-1", TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.SetNumber.ShouldBe("75192-1");
    }

    [Fact]
    public async Task GetSetAsync_WhenSetNotFound_ReturnsNullAsync() {
        // Arrange
        _httpHandler.SetupResponse(
            $"https://rebrickable.com/api/v3/lego/sets/99999-1/?key={_testApiKey}",
            statusCode: HttpStatusCode.NotFound);

        // Act
        var result = await _sut.GetSetAsync("99999", TestCancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetSetAsync_WhenRequestFails_ReturnsNullAsync() {
        // Arrange
        _httpHandler.SetupResponse(
            $"https://rebrickable.com/api/v3/lego/sets/error-1/?key={_testApiKey}",
            statusCode: HttpStatusCode.InternalServerError);

        // Act
        var result = await _sut.GetSetAsync("error", TestCancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SearchSetsAsync_WhenSetsFound_ReturnsSearchResultAsync() {
        // Arrange
        var searchResult = new RebrickableSearchResult<RebrickableSet> {
            Count = 2,
            Results = [
                new RebrickableSet { SetNumber = "75192-1", Name = "Millennium Falcon" },
                new RebrickableSet { SetNumber = "10179-1", Name = "Millennium Falcon - UCS" }
            ]
        };

        _httpHandler.SetupResponse(
            $"https://rebrickable.com/api/v3/lego/sets/?key={_testApiKey}&search=millennium&page=1&page_size=20",
            JsonSerializer.Serialize(searchResult));

        // Act
        var filters = new SetSearchFilters { Query = "millennium" };
        var result = await _sut.SearchSetsAsync(filters, TestCancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result.Results.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SearchSetsAsync_WithPagination_SendsCorrectParametersAsync() {
        // Arrange
        var searchResult = new RebrickableSearchResult<RebrickableSet> {
            Count = 100,
            Next = "next-page-url",
            Results = [new RebrickableSet { SetNumber = "1-1" }]
        };

        _httpHandler.SetupResponse(
            $"https://rebrickable.com/api/v3/lego/sets/?key={_testApiKey}&search=star%20wars&page=3&page_size=50",
            JsonSerializer.Serialize(searchResult));

        // Act
        var filters = new SetSearchFilters { Query = "star wars", Page = 3, PageSize = 50 };
        var result = await _sut.SearchSetsAsync(filters, TestCancellationToken);

        // Assert
        result.Count.ShouldBe(100);
        result.Next.ShouldNotBeNull();
    }

    [Fact]
    public async Task SearchSetsAsync_WhenRequestFails_ReturnsEmptyResultAsync() {
        // Arrange
        _httpHandler.SetupResponse(
            $"https://rebrickable.com/api/v3/lego/sets/?key={_testApiKey}&search=error&page=1&page_size=20",
            statusCode: HttpStatusCode.InternalServerError);

        // Act
        var filters = new SetSearchFilters { Query = "error" };
        var result = await _sut.SearchSetsAsync(filters, TestCancellationToken);

        // Assert
        result.Results.ShouldBeEmpty();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetThemesAsync_WhenThemesExist_ReturnsThemesAsync() {
        // Arrange
        var themesResult = new RebrickableSearchResult<RebrickableTheme> {
            Count = 3,
            Results = [
                new RebrickableTheme { Id = 1, Name = "Technic" },
                new RebrickableTheme { Id = 158, Name = "Star Wars" },
                new RebrickableTheme { Id = 246, Name = "Architecture" }
            ]
        };

        _httpHandler.SetupResponse(
            $"https://rebrickable.com/api/v3/lego/themes/?key={_testApiKey}&page=1&page_size=100",
            JsonSerializer.Serialize(themesResult));

        // Act
        var result = await _sut.GetThemesAsync(cancellationToken: TestCancellationToken);

        // Assert
        result.Count.ShouldBe(3);
        result.Results.Count.ShouldBe(3);
        result.Results.ShouldContain(t => t.Name == "Star Wars");
    }

    [Fact]
    public async Task GetThemesAsync_WithCustomPagination_SendsCorrectParametersAsync() {
        // Arrange
        var themesResult = new RebrickableSearchResult<RebrickableTheme> {
            Count = 500,
            Next = "next-page",
            Results = [new RebrickableTheme { Id = 1, Name = "Theme" }]
        };

        _httpHandler.SetupResponse(
            $"https://rebrickable.com/api/v3/lego/themes/?key={_testApiKey}&page=2&page_size=200",
            JsonSerializer.Serialize(themesResult));

        // Act
        var result = await _sut.GetThemesAsync(page: 2, pageSize: 200, cancellationToken: TestCancellationToken);

        // Assert
        result.Count.ShouldBe(500);
    }

    [Fact]
    public async Task GetThemesAsync_WhenRequestFails_ReturnsEmptyResultAsync() {
        // Arrange
        _httpHandler.SetupResponse(
            $"https://rebrickable.com/api/v3/lego/themes/?key={_testApiKey}&page=1&page_size=100",
            statusCode: HttpStatusCode.InternalServerError);

        // Act
        var result = await _sut.GetThemesAsync(cancellationToken: TestCancellationToken);

        // Assert
        result.Results.ShouldBeEmpty();
        result.Count.ShouldBe(0);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler {
        private readonly Dictionary<string, (string? Content, HttpStatusCode StatusCode)> _responses = [];

        public void SetupResponse(string url, string? content = null, HttpStatusCode statusCode = HttpStatusCode.OK) {
            // Normalize URL for consistent matching
            _responses[NormalizeUrl(url)] = (content, statusCode);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            var url = NormalizeUrl(request.RequestUri?.AbsoluteUri ?? string.Empty);

            if(_responses.TryGetValue(url, out var setup)) {
                var response = new HttpResponseMessage(setup.StatusCode);
                if(setup.Content != null) {
                    response.Content = new StringContent(setup.Content, System.Text.Encoding.UTF8, "application/json");
                }

                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static string NormalizeUrl(string url) =>
            Uri.UnescapeDataString(url);
    }
}
