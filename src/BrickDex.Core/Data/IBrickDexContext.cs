using BrickDex.Core.Models;
using BrickDex.Core.Models.Rebrickable;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Core.Data;

public interface IBrickDexContext {
    DbSet<User> Users { get; }
    DbSet<UserLogin> UserLogins { get; }
    DbSet<UserSet> UserSets { get; }

    DbSet<RebrickableTheme> RebrickableThemes { get; }
    DbSet<RebrickableSet> RebrickableSets { get; }
    DbSet<RebrickableMinifig> RebrickableMinifigs { get; }
    DbSet<RebrickableInventory> RebrickableInventories { get; }
    DbSet<RebrickableInventoryMinifig> RebrickableInventoryMinifigs { get; }
    DbSet<RebrickableInventorySet> RebrickableInventorySets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
