using System.Security.Claims;
using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrickDex.Core.Services;

public class UserService : IUserService {
    private readonly IBrickDexContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IBrickDexContext context,
        TimeProvider timeProvider,
        ILogger<UserService> logger) {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<(User? User, bool RequiresInvite)> GetOrCreateFromExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string? displayName,
        string? avatarUrl,
        string? inviteCode,
        CancellationToken cancellationToken = default) {
        // Check for existing login - existing users can always sign in
        var existingLogin = await _context.UserLogins
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == providerKey, cancellationToken);

        if(existingLogin != null) {
            return (existingLogin.User, false);
        }

        // Check for existing user by email (link accounts - no invite needed)
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        if(existingUser != null) {
            // Add new login to existing user
            var newLogin = new UserLogin {
                Id = Guid.NewGuid(),
                UserId = existingUser.Id,
                Provider = provider,
                ProviderKey = providerKey,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.UserLogins.Add(newLogin);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Linked {Provider} login to existing user {Email}", provider, email);
            return (existingUser, false);
        }

        // NEW USER REGISTRATION - Check invite requirement
        var isFirstUser = !await _context.Users.AnyAsync(cancellationToken);

        Invite? invite = null;
        if(!isFirstUser) {
            // Need valid invite for subsequent users
            if(string.IsNullOrEmpty(inviteCode)) {
                _logger.LogInformation(
                    "Registration blocked for {Email}: no invite code provided",
                    email);
                return (null, true);
            }

            invite = await _context.Invites
                .FirstOrDefaultAsync(i =>
                    i.Code == inviteCode &&
                    i.UsedByUserId == null &&
                    i.ExpiresAt > now,
                    cancellationToken);

            if(invite == null) {
                _logger.LogInformation(
                    "Registration blocked for {Email}: invalid or expired invite code",
                    email);
                return (null, true);
            }
        }

        // Create new user
        var user = new User {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            CreatedAt = now,
            UpdatedAt = now
        };

        var login = new UserLogin {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = provider,
            ProviderKey = providerKey,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Users.Add(user);
        _context.UserLogins.Add(login);

        // Consume the invite if one was used
        if(invite != null) {
            invite.UsedByUserId = user.Id;
            invite.UsedAt = now;
            invite.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        if(isFirstUser) {
            _logger.LogInformation(
                "Created first user {Email} via {Provider}",
                email,
                provider);
        } else {
            _logger.LogInformation(
                "Created new user {Email} via {Provider} using invite {InviteCode}",
                email,
                provider,
                inviteCode);
        }

        return (user, false);
    }

    public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default) {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
            return null;
        }

        return await GetByIdAsync(userId, cancellationToken);
    }
}
