using System.Security.Claims;
using BrickDex.Core.Models;

namespace BrickDex.Core.Contracts;

public interface IUserService {
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetOrCreateFromExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string? displayName,
        string? avatarUrl,
        CancellationToken cancellationToken = default);

    Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
