using BrickDex.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Core.Data;

public interface IBrickDexContext {
    DbSet<LegoSet> LegoSets { get; }
    DbSet<User> Users { get; }
    DbSet<UserLogin> UserLogins { get; }
    DbSet<UserSet> UserSets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
