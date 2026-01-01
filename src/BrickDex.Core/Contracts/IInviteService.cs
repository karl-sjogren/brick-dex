using BrickDex.Core.Models;

namespace BrickDex.Core.Contracts;

public interface IInviteService {
    /// <summary>
    /// Creates a new invite code for the specified user.
    /// </summary>
    Task<Invite> CreateInviteAsync(
        Guid createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all invites created by a specific user.
    /// </summary>
    Task<IReadOnlyList<Invite>> GetInvitesByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an invite code and returns the invite if valid.
    /// Returns null if the code is invalid, expired, or already used.
    /// </summary>
    Task<Invite?> GetValidInviteAsync(
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an invite as used by a specific user.
    /// </summary>
    Task ConsumeInviteAsync(
        string code,
        Guid usedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any users exist in the database (first user check).
    /// </summary>
    Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken = default);
}
