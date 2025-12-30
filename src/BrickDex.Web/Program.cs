using BrickDex.Web.Data;
using BrickDex.Web.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shorthand.Vite;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/brickdex-web.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try {
    Log.Information("Starting BrickDex web application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog();

    // Add database context
    builder.Services.AddDbContext<BrickDexContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=brickdex.db"));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    // Add services
    builder.Services.AddRebrickableClient(builder.Configuration);
    builder.Services.AddBrickDexServices();

    // Add Vite integration
    builder.Services.AddVite(options => {
        options.ManifestFileName = ".vite/manifest.json";
        options.Hostname = "localhost";
        options.Port = 5010;
        options.Https = true;
    });

    // Add Razor Pages
    builder.Services.AddRazorPages();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if(app.Environment.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
        app.UseMigrationsEndPoint();
    } else {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthorization();

    app.UseSerilogRequestLogging();

    app.MapRazorPages();

    // Ensure database is created
    using(var scope = app.Services.CreateScope()) {
        var context = scope.ServiceProvider.GetRequiredService<BrickDexContext>();
        await context.Database.EnsureCreatedAsync();
    }

    await app.RunAsync();
} catch(Exception ex) {
    Log.Fatal(ex, "Application terminated unexpectedly");
} finally {
    await Log.CloseAndFlushAsync();
}
