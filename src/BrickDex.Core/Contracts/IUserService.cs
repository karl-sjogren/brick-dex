using System.Security.Claims;
using BrickDex.Core.Models;

namespace BrickDex.Core.Contracts;

public interface IUserService {
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an existing user or creates a new one from external login.
    /// Returns the user and whether an invite was required but not provided.
    /// </summary>
    /// <returns>
    /// A tuple containing the User (null if registration blocked) and
    /// RequiresInvite (true if new user registration was blocked due to
    /// missing/invalid invite).
    /// </returns>
    Task<(User? User, bool RequiresInvite)> GetOrCreateFromExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string? displayName,
        string? avatarUrl,
        string? inviteCode,
        CancellationToken cancellationToken = default);

    Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
