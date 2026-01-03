using System.Security.Claims;
using BrickDex.Core.Data;
using BrickDex.Core.Models;
using BrickDex.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Core.Tests.Services;

public class UserServiceTests : IDisposable {
    private readonly BrickDexContext _context;
    private readonly FakeTimeProvider _timeProvider;
    private readonly UserService _sut;

    public UserServiceTests() {
        var options = new DbContextOptionsBuilder<BrickDexContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BrickDexContext(options);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));

        var logger = new NullLogger<UserService>();
        _sut = new UserService(_context, _timeProvider, logger);
    }

    public void Dispose() {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    // GetByIdAsync tests

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ReturnsUserAsync() {
        // Arrange
        var user = await SeedUserAsync("test@example.com");

        // Act
        var result = await _sut.GetByIdAsync(user.Id, TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Email.ShouldBe("test@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonexistentId_ReturnsNullAsync() {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), TestCancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    // GetByEmailAsync tests

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ReturnsUserAsync() {
        // Arrange
        await SeedUserAsync("test@example.com");

        // Act
        var result = await _sut.GetByEmailAsync("test@example.com", TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Email.ShouldBe("test@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonexistentEmail_ReturnsNullAsync() {
        // Act
        var result = await _sut.GetByEmailAsync("nonexistent@example.com", TestCancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    // GetOrCreateFromExternalLoginAsync tests - existing login

    [Fact]
    public async Task GetOrCreateFromExternalLoginAsync_WithExistingLogin_ReturnsExistingUserAsync() {
        // Arrange
        var user = await SeedUserAsync("test@example.com");
        await SeedUserLoginAsync(user.Id, "Google", "google-123");

        // Act
        var (result, requiresInvite) = await _sut.GetOrCreateFromExternalLoginAsync(
            "Google", "google-123", "test@example.com", "Test User", null, null, TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
        requiresInvite.ShouldBeFalse();
    }

    // GetOrCreateFromExternalLoginAsync tests - account linking

    [Fact]
    public async Task GetOrCreateFromExternalLoginAsync_WithExistingEmail_LinksAccountAsync() {
        // Arrange
        var user = await SeedUserAsync("test@example.com");
        await SeedUserLoginAsync(user.Id, "Google", "google-123");

        // Act - Login with different provider but same email
        var (result, requiresInvite) = await _sut.GetOrCreateFromExternalLoginAsync(
            "GitHub", "github-456", "test@example.com", "Test User", null, null, TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
        requiresInvite.ShouldBeFalse();

        // Verify new login was added
        var logins = await _context.UserLogins.Where(l => l.UserId == user.Id).ToListAsync(TestCancellationToken);
        logins.Count.ShouldBe(2);
        logins.ShouldContain(l => l.Provider == "GitHub" && l.ProviderKey == "github-456");
    }

    // GetOrCreateFromExternalLoginAsync tests - first user (no invite required)

    [Fact]
    public async Task GetOrCreateFromExternalLoginAsync_FirstUser_CreatesWithoutInviteAsync() {
        // Act
        var (result, requiresInvite) = await _sut.GetOrCreateFromExternalLoginAsync(
            "Google", "google-123", "first@example.com", "First User", "https://avatar.url", null, TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Email.ShouldBe("first@example.com");
        result.DisplayName.ShouldBe("First User");
        result.AvatarUrl.ShouldBe("https://avatar.url");
        requiresInvite.ShouldBeFalse();

        // Verify user and login were created
        var dbUser = await _context.Users.FindAsync([result.Id], TestCancellationToken);
        dbUser.ShouldNotBeNull();

        var login = await _context.UserLogins.FirstOrDefaultAsync(l => l.UserId == result.Id, TestCancellationToken);
        login.ShouldNotBeNull();
        login.Provider.ShouldBe("Google");
    }

    // GetOrCreateFromExternalLoginAsync tests - subsequent user requires invite

    [Fact]
    public async Task GetOrCreateFromExternalLoginAsync_SubsequentUserNoInvite_RequiresInviteAsync() {
        // Arrange - create first user
        await SeedUserAsync("first@example.com");

        // Act - try to create second user without invite
        var (result, requiresInvite) = await _sut.GetOrCreateFromExternalLoginAsync(
            "Google", "google-456", "second@example.com", "Second User", null, null, TestCancellationToken);

        // Assert
        result.ShouldBeNull();
        requiresInvite.ShouldBeTrue();
    }

    [Fact]
    public async Task GetOrCreateFromExternalLoginAsync_SubsequentUserWithInvalidInvite_RequiresInviteAsync() {
        // Arrange
        await SeedUserAsync("first@example.com");

        // Act
        var (result, requiresInvite) = await _sut.GetOrCreateFromExternalLoginAsync(
            "Google", "google-456", "second@example.com", "Second User", null, "invalid-code", TestCancellationToken);

        // Assert
        result.ShouldBeNull();
        requiresInvite.ShouldBeTrue();
    }

    [Fact]
    public async Task GetOrCreateFromExternalLoginAsync_SubsequentUserWithExpiredInvite_RequiresInviteAsync() {
        // Arrange
        var firstUser = await SeedUserAsync("first@example.com");
        var invite = await SeedInviteAsync(firstUser.Id, "test-invite", expiresIn: TimeSpan.FromDays(-1));

        // Act
        var (result, requiresInvite) = await _sut.GetOrCreateFromExternalLoginAsync(
            "Google", "google-456", "second@example.com", "Second User", null, invite.Code, TestCancellationToken);

        // Assert
        result.ShouldBeNull();
        requiresInvite.ShouldBeTrue();
    }

    [Fact]
    public async Task GetOrCreateFromExternalLoginAsync_SubsequentUserWithUsedInvite_RequiresInviteAsync() {
        // Arrange
        var firstUser = await SeedUserAsync("first@example.com");
        var invite = await SeedInviteAsync(firstUser.Id, "test-invite", usedByUserId: Guid.NewGuid());

        // Act
        var (result, requiresInvite) = await _sut.GetOrCreateFromExternalLoginAsync(
            "Google", "google-456", "second@example.com", "Second User", null, invite.Code, TestCancellationToken);

        // Assert
        result.ShouldBeNull();
        requiresInvite.ShouldBeTrue();
    }

    [Fact]
    public async Task GetOrCreateFromExternalLoginAsync_SubsequentUserWithValidInvite_CreatesUserAsync() {
        // Arrange
        var firstUser = await SeedUserAsync("first@example.com");
        var invite = await SeedInviteAsync(firstUser.Id, "test-invite");

        // Act
        var (result, requiresInvite) = await _sut.GetOrCreateFromExternalLoginAsync(
            "Google", "google-456", "second@example.com", "Second User", null, invite.Code, TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Email.ShouldBe("second@example.com");
        requiresInvite.ShouldBeFalse();
    }

    [Fact]
    public async Task GetOrCreateFromExternalLoginAsync_WithValidInvite_ConsumesInviteAsync() {
        // Arrange
        var firstUser = await SeedUserAsync("first@example.com");
        var invite = await SeedInviteAsync(firstUser.Id, "test-invite");

        // Act
        var (result, _) = await _sut.GetOrCreateFromExternalLoginAsync(
            "Google", "google-456", "second@example.com", "Second User", null, invite.Code, TestCancellationToken);

        // Assert
        var dbInvite = await _context.Invites.FindAsync([invite.Id], TestCancellationToken);
        dbInvite!.UsedByUserId.ShouldBe(result!.Id);
        dbInvite.UsedAt.ShouldNotBeNull();
    }

    // GetCurrentUserAsync tests

    [Fact]
    public async Task GetCurrentUserAsync_WithValidClaims_ReturnsUserAsync() {
        // Arrange
        var user = await SeedUserAsync("test@example.com");
        var principal = CreateClaimsPrincipal(user.Id);

        // Act
        var result = await _sut.GetCurrentUserAsync(principal, TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithNoNameIdentifierClaim_ReturnsNullAsync() {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await _sut.GetCurrentUserAsync(principal, TestCancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithInvalidGuid_ReturnsNullAsync() {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var result = await _sut.GetCurrentUserAsync(principal, TestCancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithNonexistentUserId_ReturnsNullAsync() {
        // Arrange
        var principal = CreateClaimsPrincipal(Guid.NewGuid());

        // Act
        var result = await _sut.GetCurrentUserAsync(principal, TestCancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    // Helper methods

    private async Task<User> SeedUserAsync(string email) {
        var user = new User {
            Id = Guid.NewGuid(),
            Email = email,
            CreatedAt = _timeProvider.GetUtcNow(),
            UpdatedAt = _timeProvider.GetUtcNow()
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(TestCancellationToken);
        return user;
    }

    private async Task SeedUserLoginAsync(Guid userId, string provider, string providerKey) {
        var login = new UserLogin {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ProviderKey = providerKey,
            CreatedAt = _timeProvider.GetUtcNow(),
            UpdatedAt = _timeProvider.GetUtcNow()
        };
        _context.UserLogins.Add(login);
        await _context.SaveChangesAsync(TestCancellationToken);
    }

    private async Task<Invite> SeedInviteAsync(Guid createdByUserId, string code, TimeSpan? expiresIn = null, Guid? usedByUserId = null) {
        var now = _timeProvider.GetUtcNow();
        var invite = new Invite {
            Id = Guid.NewGuid(),
            Code = code,
            CreatedByUserId = createdByUserId,
            ExpiresAt = now.Add(expiresIn ?? TimeSpan.FromDays(7)),
            UsedByUserId = usedByUserId,
            UsedAt = usedByUserId.HasValue ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Invites.Add(invite);
        await _context.SaveChangesAsync(TestCancellationToken);
        return invite;
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(Guid userId) {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
