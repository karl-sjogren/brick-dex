using System.IO.Abstractions;
using BrickDex.Core.Contracts;
using BrickDex.Lucene.Index;
using BrickDex.Lucene.Options;
using BrickDex.Lucene.Search;
using BrickDex.Lucene.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BrickDex.Lucene.Extensions;

/// <summary>
/// Extension methods for registering Lucene search services.
/// </summary>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// Adds Lucene search services to the service collection.
    /// </summary>
    public static IServiceCollection AddLuceneSearch(
        this IServiceCollection services,
        IConfiguration configuration) {
        // Configure options
        services.AddOptions<LuceneOptions>()
            .Bind(configuration.GetSection(LuceneOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register file system abstraction if not already registered
        services.TryAddSingleton<IFileSystem, FileSystem>();

        // Register services
        services.AddSingleton<IndexStorageFactory>();
        services.AddSingleton<LuceneSearchIndex>();
        services.AddSingleton<ISearchIndex>(sp => sp.GetRequiredService<LuceneSearchIndex>());
        services.AddSingleton<ISearchService, LuceneSearchService>();

        // Register hosted service to initialize index on startup
        services.AddHostedService<IndexInitializationService>();

        return services;
    }
}
