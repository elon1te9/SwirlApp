using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Swirl.Api.Data;
using Swirl.Api.Models;
using Swirl.Api.Requests;
using Swirl.Api.Services;

namespace Swirl.Api.Tests;

public class ProfileServiceTests
{
    [Fact]
    public async Task GetAvatarsAsync_ReturnsOnlyActiveAvatars()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var avatar = await dbContext.Avatars.SingleAsync(candidate => candidate.Id == 2);
        avatar.IsActive = false;
        await dbContext.SaveChangesAsync();
        var profileService = new ProfileService(dbContext);

        var result = await profileService.GetAvatarsAsync();

        Assert.Equal([1, 3, 4], result.Select(candidate => candidate.Id).ToArray());
        Assert.All(result, candidate => Assert.StartsWith("/media/avatars/", candidate.ImageUrl));
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsCurrentUsersProfileSummary()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithProfileAsync(dbContext, "first@example.com", "Vladimir", 1);
        var otherUser = await CreateUserWithProfileAsync(dbContext, "second@example.com", "Other", 2);
        var firstFoodLevel = await dbContext.Levels
            .Include(level => level.Section)
            .Where(level => level.Section.Title == "Food")
            .OrderBy(level => level.SortOrder)
            .FirstAsync();
        var secondFoodLevel = await dbContext.Levels
            .Include(level => level.Section)
            .Where(level => level.Section.Title == "Food")
            .OrderBy(level => level.SortOrder)
            .Skip(1)
            .FirstAsync();
        var word = new Word
        {
            LevelId = firstFoodLevel.Id,
            English = "apple",
            Russian = "apple",
            CefrLevel = "A1",
            IsActive = true,
            CreatedAt = CreateTimestamp()
        };
        dbContext.Words.Add(word);
        await dbContext.SaveChangesAsync();

        var profile = await dbContext.UserProfiles.SingleAsync(candidate => candidate.UserId == user.Id);
        profile.CurrentStreak = 4;
        profile.BestStreak = 7;

        var userFoodProgress = await dbContext.UserLevelProgresses
            .SingleAsync(progress => progress.UserId == user.Id && progress.LevelId == firstFoodLevel.Id);
        userFoodProgress.Status = "completed";

        var otherUserFoodProgress = await dbContext.UserLevelProgresses
            .SingleAsync(progress => progress.UserId == otherUser.Id && progress.LevelId == secondFoodLevel.Id);
        otherUserFoodProgress.Status = "completed";
        dbContext.UserWordProgresses.Add(new UserWordProgress
        {
            UserId = user.Id,
            WordId = word.Id,
            LearnedAt = CreateTimestamp()
        });
        await dbContext.SaveChangesAsync();
        var profileService = new ProfileService(dbContext);

        var result = await profileService.GetProfileAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal("Vladimir", result.Name);
        Assert.Equal("/media/avatars/avatar_1.png", result.AvatarUrl);
        Assert.Equal(4, result.CurrentStreak);
        Assert.Equal(7, result.BestStreak);
        Assert.Equal(1, result.LearnedWordsCount);
        Assert.Equal(1, result.CompletedLevelsCount);

        var foodProgress = Assert.Single(result.SectionsProgress, section => section.Title == "Food");
        Assert.Equal(firstFoodLevel.SectionId, foodProgress.SectionId);
        Assert.Equal(17, foodProgress.ProgressPercent);
    }

    [Fact]
    public async Task ChangeAvatarAsync_UpdatesOnlyCurrentUsersAvatar()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithProfileAsync(dbContext, "first@example.com", "Vladimir", 1);
        var otherUser = await CreateUserWithProfileAsync(dbContext, "second@example.com", "Other", 3);
        var profileService = new ProfileService(dbContext);

        var result = await profileService.ChangeAvatarAsync(user.Id, new ChangeAvatarRequest
        {
            AvatarId = 2
        });

        var profile = await dbContext.UserProfiles.SingleAsync(candidate => candidate.UserId == user.Id);
        var otherProfile = await dbContext.UserProfiles.SingleAsync(candidate => candidate.UserId == otherUser.Id);

        Assert.Equal("/media/avatars/avatar_2.png", result.AvatarUrl);
        Assert.Equal(2, profile.AvatarId);
        Assert.NotNull(profile.UpdatedAt);
        Assert.Equal(3, otherProfile.AvatarId);
    }

    [Fact]
    public async Task ChangeAvatarAsync_InactiveAvatarThrowsValidationError()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithProfileAsync(dbContext, "first@example.com", "Vladimir", 1);
        var avatar = await dbContext.Avatars.SingleAsync(candidate => candidate.Id == 2);
        avatar.IsActive = false;
        await dbContext.SaveChangesAsync();
        var profileService = new ProfileService(dbContext);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            profileService.ChangeAvatarAsync(user.Id, new ChangeAvatarRequest
            {
                AvatarId = 2
            }));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Equal("validation_error", exception.Code);
        Assert.Equal(["Avatar must exist and be active"], exception.Details!["avatarId"]);
    }

    private static async Task<User> CreateUserWithProfileAsync(
        AppDbContext dbContext,
        string email,
        string name,
        int avatarId)
    {
        var now = CreateTimestamp();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "hash",
            CreatedAt = now
        };

        dbContext.Users.Add(user);
        dbContext.UserProfiles.Add(new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = name,
            AvatarId = avatarId,
            CreatedAt = now
        });

        var levels = await dbContext.Levels
            .Include(level => level.Section)
            .Where(level => level.IsActive && level.Section.IsActive)
            .ToListAsync();

        dbContext.UserLevelProgresses.AddRange(levels.Select(level => new UserLevelProgress
        {
            UserId = user.Id,
            LevelId = level.Id,
            Status = "locked",
            WordsLearned = false,
            AttemptsCount = 0
        }));

        await dbContext.SaveChangesAsync();

        return user;
    }

    private static async Task<AppDbContext> CreateSeededDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);
        await DatabaseSeeder.SeedAsync(dbContext);

        return dbContext;
    }

    private static DateTime CreateTimestamp() =>
        DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}
