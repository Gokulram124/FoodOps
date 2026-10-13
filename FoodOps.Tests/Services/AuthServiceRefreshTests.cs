using System.Security.Cryptography;
using System.Text;
using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Auth;
using FoodOps.Infrastructure.Persistence;
using FoodOps.Infrastructure.Services;
using FoodOps.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace FoodOps.Tests.Services;

public class AuthServiceRefreshTests : IDisposable
{
    private readonly TestDb _test = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 13, 10, 0, 0, TimeSpan.Zero));
    private readonly ApplicationUser _user = new()
    {
        Id = Guid.NewGuid(),
        UserName = "c@foodops.test",
        Email = "c@foodops.test",
        FullName = "Test Customer"
    };
    private readonly AuthService _sut;

    public AuthServiceRefreshTests()
    {
        // UserManager has no parameterless constructor; this is the standard way to mock it
        var users = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        users.Setup(m => m.FindByEmailAsync(_user.Email!)).ReturnsAsync(_user);
        users.Setup(m => m.FindByIdAsync(_user.Id.ToString())).ReturnsAsync(_user);
        users.Setup(m => m.CheckPasswordAsync(_user, "pw")).ReturnsAsync(true);
        users.Setup(m => m.GetRolesAsync(_user)).ReturnsAsync((IList<string>)new List<string> { Roles.Customer });

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-only-key-test-only-key-test-only-key-1234",   // dummy value, used only in tests
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test",
            ["Jwt:ExpiryMinutes"] = "15",
            ["Jwt:RefreshTokenDays"] = "7"
        }).Build();

        _sut = new AuthService(users.Object, config, _test.Db, _clock);
    }

    public void Dispose() => _test.Dispose();

    private static string Sha(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    // every real HTTP request gets a fresh DbContext; clearing the tracker imitates that
    private void NewRequest() => _test.Db.ChangeTracker.Clear();

    private Task<AuthResponse> LoginAsync() => _sut.LoginAsync(new LoginDto("c@foodops.test", "pw"));

    [Fact]
    public async Task Login_returns_a_refresh_token_and_stores_only_its_hash()
    {
        var result = await LoginAsync();

        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        var row = await _test.Db.RefreshTokens.AsNoTracking().SingleAsync();
        Assert.Equal(Sha(result.RefreshToken), row.TokenHash);
        Assert.NotEqual(result.RefreshToken, row.TokenHash);
        Assert.Equal(_user.Id, row.UserId);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime.AddMinutes(15), result.ExpiresAt);   // deterministic thanks to FakeTimeProvider
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_retires_the_old_one()
    {
        var first = await LoginAsync();
        NewRequest();

        var second = await _sut.RefreshAsync(first.RefreshToken);

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        var rows = await _test.Db.RefreshTokens.AsNoTracking().OrderBy(t => t.Id).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.NotNull(rows[0].RevokedAt);
        Assert.Equal(Sha(second.RefreshToken), rows[0].ReplacedByHash);
        Assert.Null(rows[1].RevokedAt);
    }

    [Fact]
    public async Task Reusing_an_old_token_revokes_the_whole_session()
    {
        var first = await LoginAsync();
        NewRequest();
        var second = await _sut.RefreshAsync(first.RefreshToken);

        NewRequest();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RefreshAsync(first.RefreshToken));   // replay of the old token

        NewRequest();
        // the legitimate newest token is dead too, because we cannot tell the thief from the owner
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RefreshAsync(second.RefreshToken));

        var rows = await _test.Db.RefreshTokens.AsNoTracking().ToListAsync();
        Assert.All(rows, r => Assert.NotNull(r.RevokedAt));
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        var first = await LoginAsync();
        NewRequest();

        _clock.Advance(TimeSpan.FromDays(8));   // lifetime is 7 days

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RefreshAsync(first.RefreshToken));
    }

    [Fact]
    public async Task Unknown_token_is_rejected()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RefreshAsync("not-a-real-token"));
    }

    [Fact]
    public async Task Logout_revokes_the_token()
    {
        var first = await LoginAsync();
        NewRequest();

        await _sut.LogoutAsync(first.RefreshToken);
        NewRequest();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RefreshAsync(first.RefreshToken));
    }

    [Fact]
    public async Task Logout_with_an_unknown_token_is_harmless()
    {
        var ex = await Record.ExceptionAsync(() => _sut.LogoutAsync("whatever"));

        Assert.Null(ex);
    }
}