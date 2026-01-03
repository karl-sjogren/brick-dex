using BrickDex.Core.Data;
using BrickDex.Core.Models;
using BrickDex.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Core.Tests.Services;

public class InviteServiceTests : IDisposable {
    private readonly BrickDexContext _context;
    private readonly FakeTimeProvider _timeProvider;
    private readonly InviteService _sut;

    private readonly Guid _testUserId = Guid.NewGuid();

    public InviteServiceTests() {
        var options = new DbContextOptionsBuilder<BrickDexContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BrickDexContext(options);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));

        var logger = new NullLogger<InviteService>();
        _sut = new InviteService(_context, _timeProvider, logger);
    }

    public void Dispose() {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    // CreateInviteAsync tests

    [Fact]
    public async Task CreateInviteAsync_CreatesInviteWithGeneratedCodeAsync() {
        // Act
        var result = await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldNotBeNullOrEmpty();
        result.Code.Length.ShouldBe(16); // Base64 of 12 bytes = 16 chars
        result.CreatedByUserId.ShouldBe(_testUserId);
    }

    [Fact]
    public async Task CreateInviteAsync_SetsExpirationToSevenDaysAsync() {
        // Act
        var result = await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);

        // Assert
        var expectedExpiry = _timeProvider.GetUtcNow().AddDays(7);
        result.ExpiresAt.ShouldBe(expectedExpiry);
    }

    [Fact]
    public async Task CreateInviteAsync_SetsTimestampsAsync() {
        // Act
        var result = await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);

        // Assert
        result.CreatedAt.ShouldBe(_timeProvider.GetUtcNow());
        result.UpdatedAt.ShouldBe(_timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task CreateInviteAsync_PersistsToDbAsync() {
        // Act
        var result = await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);

        // Assert
        var dbInvite = await _context.Invites.FindAsync([result.Id], TestCancellationToken);
        dbInvite.ShouldNotBeNull();
        dbInvite.Code.ShouldBe(result.Code);
    }

    [Fact]
    public async Task CreateInviteAsync_GeneratesUniqueCodesAsync() {
        // Act
        var invite1 = await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);
        var invite2 = await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);

        // Assert
        invite1.Code.ShouldNotBe(invite2.Code);
    }

    // GetInvitesByUserAsync tests

    [Fact]
    public async Task GetInvitesByUserAsync_ReturnsOnlyUserInvitesAsync() {
        // Arrange
        var otherUserId = Guid.NewGuid();
        await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);
        await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);
        await _sut.CreateInviteAsync(otherUserId, TestCancellationToken);

        // Act
        var result = await _sut.GetInvitesByUserAsync(_testUserId, TestCancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(i => i.CreatedByUserId == _testUserId);
    }

    [Fact]
    public async Task GetInvitesByUserAsync_ReturnsInDescendingOrderAsync() {
        // Arrange
        await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);
        _timeProvider.Advance(TimeSpan.FromHours(1));
        await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);
        _timeProvider.Advance(TimeSpan.FromHours(1));
        await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);

        // Act
        var result = await _sut.GetInvitesByUserAsync(_testUserId, TestCancellationToken);

        // Assert
        result[0].CreatedAt.ShouldBeGreaterThan(result[1].CreatedAt);
        result[1].CreatedAt.ShouldBeGreaterThan(result[2].CreatedAt);
    }

    // GetValidInviteAsync tests

    [Fact]
    public async Task GetValidInviteAsync_WithValidCode_ReturnsInviteAsync() {
        // Arrange
        var invite = await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);

        // Act
        var result = await _sut.GetValidInviteAsync(invite.Code, TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe(invite.Code);
    }

    [Fact]
    public async Task GetValidInviteAsync_WithExpiredInvite_ReturnsNullAsync() {
        // Arrange
        var invite = await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);
        _timeProvider.Advance(TimeSpan.FromDays(8)); // Past 7-day expiration

        // Act
        var result = await _sut.GetValidInviteAsync(invite.Code, TestCancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetValidInviteAsync_WithUsedInvite_ReturnsNullAsync() {
        // Arrange
        var invite = await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);
        var usedByUserId = Guid.NewGuid();
        await _sut.ConsumeInviteAsync(invite.Code, usedByUserId, TestCancellationToken);

        // Act
        var result = await _sut.GetValidInviteAsync(invite.Code, TestCancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetValidInviteAsync_WithInvalidCode_ReturnsNullAsync() {
        // Act
        var result = await _sut.GetValidInviteAsync("invalid-code", TestCancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    // ConsumeInviteAsync tests

    [Fact]
    public async Task ConsumeInviteAsync_MarksInviteAsUsedAsync() {
        // Arrange
        var invite = await _sut.CreateInviteAsync(_testUserId, TestCancellationToken);
        var usedByUserId = Guid.NewGuid();
        _timeProvider.Advance(TimeSpan.FromHours(1));

        // Act
        await _sut.ConsumeInviteAsync(invite.Code, usedByUserId, TestCancellationToken);

        // Assert
        var dbInvite = await _context.Invites.FindAsync([invite.Id], TestCancellationToken);
        dbInvite!.UsedByUserId.ShouldBe(usedByUserId);
        dbInvite.UsedAt.ShouldBe(_timeProvider.GetUtcNow());
        dbInvite.UpdatedAt.ShouldBe(_timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task ConsumeInviteAsync_WithInvalidCode_ThrowsExceptionAsync() {
        // Arrange
        var usedByUserId = Guid.NewGuid();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.ConsumeInviteAsync("invalid-code", usedByUserId, TestCancellationToken));

        ex.Message.ShouldContain("not found");
    }

    // AnyUsersExistAsync tests

    [Fact]
    public async Task AnyUsersExistAsync_WithNoUsers_ReturnsFalseAsync() {
        // Act
        var result = await _sut.AnyUsersExistAsync(TestCancellationToken);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task AnyUsersExistAsync_WithUsers_ReturnsTrueAsync() {
        // Arrange
        _context.Users.Add(new User {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            CreatedAt = _timeProvider.GetUtcNow(),
            UpdatedAt = _timeProvider.GetUtcNow()
        });
        await _context.SaveChangesAsync(TestCancellationToken);

        // Act
        var result = await _sut.AnyUsersExistAsync(TestCancellationToken);

        // Assert
        result.ShouldBeTrue();
    }
}
