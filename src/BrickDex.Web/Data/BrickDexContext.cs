using BrickDex.Core.Data;
using BrickDex.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BrickDex.Web.Data;

public class BrickDexContext : DbContext, IBrickDexContext {
    public BrickDexContext(DbContextOptions<BrickDexContext> options)
        : base(options) {
    }

    public DbSet<LegoSet> LegoSets { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserLogin> UserLogins { get; set; }
    public DbSet<UserSet> UserSets { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BrickDexContext).Assembly);
    }
}
