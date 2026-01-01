using System.Security.Cryptography;
using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrickDex.Core.Services;

public class InviteService : IInviteService {
    private readonly IBrickDexContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InviteService> _logger;

    private static readonly TimeSpan _inviteExpiration = TimeSpan.FromDays(7);

    public InviteService(
        IBrickDexContext context,
        TimeProvider timeProvider,
        ILogger<InviteService> logger) {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Invite> CreateInviteAsync(
        Guid createdByUserId,
        CancellationToken cancellationToken = default) {
        var now = _timeProvider.GetUtcNow();
        var code = GenerateInviteCode();

        var invite = new Invite {
            Id = Guid.NewGuid(),
            Code = code,
            CreatedByUserId = createdByUserId,
            ExpiresAt = now.Add(_inviteExpiration),
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Invites.Add(invite);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} created invite {InviteCode}",
            createdByUserId,
            code);

        return invite;
    }

    public async Task<IReadOnlyList<Invite>> GetInvitesByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) {
        return await _context.Invites
            .AsNoTracking()
            .Include(i => i.UsedByUser)
            .Where(i => i.CreatedByUserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invite?> GetValidInviteAsync(
        string code,
        CancellationToken cancellationToken = default) {
        var now = _timeProvider.GetUtcNow();

        return await _context.Invites
            .AsNoTracking()
            .FirstOrDefaultAsync(i =>
                i.Code == code &&
                i.UsedByUserId == null &&
                i.ExpiresAt > now,
                cancellationToken);
    }

    public async Task ConsumeInviteAsync(
        string code,
        Guid usedByUserId,
        CancellationToken cancellationToken = default) {
        var now = _timeProvider.GetUtcNow();

        var invite = await _context.Invites
            .FirstOrDefaultAsync(i => i.Code == code, cancellationToken);

        if(invite == null) {
            throw new InvalidOperationException($"Invite with code {code} not found");
        }

        invite.UsedByUserId = usedByUserId;
        invite.UsedAt = now;
        invite.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Invite {InviteCode} consumed by user {UserId}",
            code,
            usedByUserId);
    }

    public async Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken = default) {
        return await _context.Users.AnyAsync(cancellationToken);
    }

    private static string GenerateInviteCode() {
        // Generate 12 random bytes = 16 base64url characters
        var bytes = RandomNumberGenerator.GetBytes(12);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
    }
}
