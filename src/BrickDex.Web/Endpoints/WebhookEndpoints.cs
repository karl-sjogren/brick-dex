using System.Diagnostics;
using BrickDex.Core.Contracts;
using BrickDex.Web.Contracts;
using BrickDex.Web.Options;
using Microsoft.Extensions.Options;

namespace BrickDex.Web.Endpoints;

public static class WebhookEndpoints {
    public static WebApplication MapWebhookEndpoints(this WebApplication app) {
        var group = app.MapGroup("/api/webhooks").WithTags("Webhooks");

        group.MapPost("/reindex", ReindexAsync);

        return app;
    }

    private static async Task<IResult> ReindexAsync(
        HttpContext httpContext,
        ISearchIndex searchIndex,
        IOptions<WebhookOptions> webhookOptions,
        ILogger<Program> logger,
        CancellationToken cancellationToken) {
        // Validate API key
        if(!httpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
            string.IsNullOrEmpty(apiKey) ||
            apiKey != webhookOptions.Value.ReindexApiKey) {
            logger.LogWarning("Unauthorized reindex webhook attempt");
            return Results.Unauthorized();
        }

        logger.LogInformation("Starting search index rebuild via webhook");
        var stopwatch = Stopwatch.StartNew();

        try {
            await searchIndex.RebuildIndexAsync(cancellationToken);
            stopwatch.Stop();

            logger.LogInformation("Search index rebuilt successfully via webhook in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);

            return Results.Ok(new ReindexResponse(
                Success: true,
                Message: "Search index rebuilt successfully",
                DurationMs: stopwatch.ElapsedMilliseconds
            ));
        } catch(Exception ex) {
            stopwatch.Stop();
            logger.LogError(ex, "Search index rebuild failed via webhook after {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);

            return Results.Problem(
                title: "Reindex failed",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }
}
