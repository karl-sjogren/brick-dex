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

    public async Task<User?> GetOrCreateFromExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string? displayName,
        string? avatarUrl,
        CancellationToken cancellationToken = default) {
        // Check for existing login
        var existingLogin = await _context.UserLogins
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == providerKey, cancellationToken);

        if(existingLogin != null) {
            return existingLogin.User;
        }

        // Check for existing user by email (link accounts)
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
            return existingUser;
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
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new user {Email} via {Provider}", email, provider);
        return user;
    }

    public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default) {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
            return null;
        }

        return await GetByIdAsync(userId, cancellationToken);
    }
}
