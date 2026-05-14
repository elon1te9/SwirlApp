using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Swirl.Api.Data;
using Swirl.Api.Interfaces;
using Swirl.Api.Models;
using Swirl.Api.Requests;
using Swirl.Api.Services;

namespace Swirl.Api.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUserProfileAndInitialLevelProgress()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var authService = CreateAuthService(dbContext);

        var result = await authService.RegisterAsync(new RegisterRequest
        {
            Name = "Vladimir",
            Email = " USER@example.COM ",
            Password = "password123",
            ConfirmPassword = "password123",
            AvatarId = 1
        });

        var user = await dbContext.Users.SingleAsync();
        var profile = await dbContext.UserProfiles.Include(userProfile => userProfile.Avatar).SingleAsync();
        var progresses = await dbContext.UserLevelProgresses
            .Include(progress => progress.Level)
            .ThenInclude(level => level.Section)
            .ToListAsync();
        var activeLevelsCount = await dbContext.Levels.CountAsync(level => level.IsActive);
        var activeSectionsCount = await dbContext.Sections.CountAsync(section => section.IsActive);

        Assert.Equal("test-jwt-token", result.AccessToken);
        Assert.Equal(user.Id.ToString(), result.User.Id);
        Assert.Equal("user@example.com", result.User.Email);
        Assert.Equal("Vladimir", result.User.Name);
        Assert.Equal("/media/avatars/avatar_1.png", result.User.AvatarUrl);
        Assert.Equal("user@example.com", user.Email);
        Assert.NotEqual("password123", user.PasswordHash);
        Assert.Equal(user.Id, profile.UserId);
        Assert.Equal(1, profile.AvatarId);
        Assert.Equal(activeLevelsCount, progresses.Count);
        Assert.Equal(activeSectionsCount, progresses.Count(progress => progress.Status == "available"));

        foreach (var sectionGroup in progresses.GroupBy(progress => progress.Level.SectionId))
        {
            var firstNormalLevel = sectionGroup
                .Where(progress => !progress.Level.IsFinalTest)
                .OrderBy(progress => progress.Level.SortOrder)
                .First();

            Assert.Equal("available", firstNormalLevel.Status);
            Assert.NotNull(firstNormalLevel.UnlockedAt);

            Assert.All(
                sectionGroup.Where(progress => progress.LevelId != firstNormalLevel.LevelId),
                progress => Assert.Equal("locked", progress.Status));
        }
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmailThrowsConflict()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var authService = CreateAuthService(dbContext);

        await authService.RegisterAsync(CreateRegisterRequest("user@example.com"));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            authService.RegisterAsync(CreateRegisterRequest(" USER@example.com ")));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.Equal("email_already_exists", exception.Code);
    }

    [Fact]
    public async Task LoginAsync_InvalidPasswordThrowsInvalidCredentials()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var authService = CreateAuthService(dbContext);

        await authService.RegisterAsync(CreateRegisterRequest("user@example.com"));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            authService.LoginAsync(new LoginRequest
            {
                Email = "user@example.com",
                Password = "wrong-password"
            }));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        Assert.Equal("invalid_credentials", exception.Code);
        Assert.Equal("Invalid email or password", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentialsReturnsCurrentUser()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var authService = CreateAuthService(dbContext);

        await authService.RegisterAsync(CreateRegisterRequest("user@example.com"));

        var result = await authService.LoginAsync(new LoginRequest
        {
            Email = " USER@example.com ",
            Password = "password123"
        });

        Assert.Equal("test-jwt-token", result.AccessToken);
        Assert.Equal("user@example.com", result.User.Email);
        Assert.Equal("Vladimir", result.User.Name);
        Assert.Equal("/media/avatars/avatar_1.png", result.User.AvatarUrl);
    }

    private static RegisterRequest CreateRegisterRequest(string email) =>
        new()
        {
            Name = "Vladimir",
            Email = email,
            Password = "password123",
            ConfirmPassword = "password123",
            AvatarId = 1
        };

    private static AuthService CreateAuthService(AppDbContext dbContext) =>
        new(dbContext, new PasswordHashService(), new TestJwtTokenService());

    private static async Task<AppDbContext> CreateSeededDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);
        await DatabaseSeeder.SeedAsync(dbContext);

        return dbContext;
    }

    private sealed class TestJwtTokenService : IJwtTokenService
    {
        public string CreateAccessToken(User user) => "test-jwt-token";
    }
}
