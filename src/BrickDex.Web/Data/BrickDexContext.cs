using BrickDex.Core.Data;
using BrickDex.Core.Models;
using BrickDex.Core.Models.Rebrickable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BrickDex.Web.Data;

public class BrickDexContext : DbContext, IBrickDexContext {
    public BrickDexContext(DbContextOptions<BrickDexContext> options)
        : base(options) {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserLogin> UserLogins { get; set; }
    public DbSet<UserSet> UserSets { get; set; }
    public DbSet<Invite> Invites { get; set; }

    public DbSet<RebrickableTheme> RebrickableThemes { get; set; }
    public DbSet<RebrickableSet> RebrickableSets { get; set; }
    public DbSet<RebrickableMinifig> RebrickableMinifigs { get; set; }
    public DbSet<RebrickableInventory> RebrickableInventories { get; set; }
    public DbSet<RebrickableInventoryMinifig> RebrickableInventoryMinifigs { get; set; }
    public DbSet<RebrickableInventorySet> RebrickableInventorySets { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BrickDexContext).Assembly);
    }
}
