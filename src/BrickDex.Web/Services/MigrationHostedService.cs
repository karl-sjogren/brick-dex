using BrickDex.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Web.Services;

public class MigrationHostedService : IHostedService {
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MigrationHostedService> _logger;

    public MigrationHostedService(
            IServiceProvider serviceProvider,
            ILogger<MigrationHostedService> logger) {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Running database migrations...");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BrickDexContext>();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
        var pendingList = pendingMigrations.ToList();

        if(pendingList.Count == 0) {
            _logger.LogInformation("No pending migrations found");
            return;
        }

        _logger.LogInformation("Found {Count} pending migrations.", pendingList.Count);

        await context.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Database migrations completed successfully");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
