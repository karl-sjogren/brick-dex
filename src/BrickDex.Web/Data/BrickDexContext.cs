using BrickDex.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BrickDex.Web.Data;

public class BrickDexContext : DbContext {
    public BrickDexContext(DbContextOptions<BrickDexContext> options)
        : base(options) {
    }

    public DbSet<LegoSet> LegoSets { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BrickDexContext).Assembly);
    }
}
