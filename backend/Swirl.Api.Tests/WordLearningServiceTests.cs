using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Swirl.Api.Data;
using Swirl.Api.Models;
using Swirl.Api.Requests;
using Swirl.Api.Services;

namespace Swirl.Api.Tests;

public class WordLearningServiceTests
{
    [Fact]
    public async Task GetLevelWordsAsync_ReturnsActiveWordsForAvailableLevel()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "user@example.com");
        var level = await GetSectionLevelAsync(dbContext, "Food", 1);
        var inactiveWord = await dbContext.Words.FirstAsync(word => word.LevelId == level.Id);
        inactiveWord.IsActive = false;
        await dbContext.SaveChangesAsync();
        var service = new WordLearningService(dbContext);

        var result = await service.GetLevelWordsAsync(user.Id, level.Id);

        Assert.Equal(4, result.Count);
        Assert.DoesNotContain(result, word => word.Id == inactiveWord.Id);
        Assert.All(result, word =>
        {
            Assert.False(string.IsNullOrWhiteSpace(word.English));
            Assert.False(string.IsNullOrWhiteSpace(word.Russian));
            Assert.StartsWith("/media/images/words/", word.ImageUrl);
            Assert.StartsWith("/media/audio/words/", word.AudioUrl);
        });
    }

    [Fact]
    public async Task GetLevelWordsAsync_ReturnsEmptyListForAvailableFinalTest()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "user@example.com");
        var finalTest = await GetSectionLevelAsync(dbContext, "Food", 6);
        await SetLevelStatusAsync(dbContext, user.Id, finalTest.Id, "available");
        var service = new WordLearningService(dbContext);

        var result = await service.GetLevelWordsAsync(user.Id, finalTest.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLevelWordsAsync_RejectsLockedLevel()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "user@example.com");
        var level = await GetSectionLevelAsync(dbContext, "Food", 2);
        var service = new WordLearningService(dbContext);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetLevelWordsAsync(user.Id, level.Id));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.Equal("level_locked", exception.Code);
    }

    [Fact]
    public async Task MarkLevelWordsLearnedAsync_CreatesMissingProgressAndMarksLevelWordsLearned()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "user@example.com");
        var level = await GetSectionLevelAsync(dbContext, "Food", 1);
        var wordIds = await GetActiveWordIdsAsync(dbContext, level.Id);
        var service = new WordLearningService(dbContext);

        var result = await service.MarkLevelWordsLearnedAsync(
            user.Id,
            level.Id,
            new MarkLevelWordsLearnedRequest { WordIds = wordIds });

        Assert.Equal(level.Id, result.LevelId);
        Assert.True(result.WordsLearned);
        Assert.Equal(wordIds.Count, result.LearnedWordsCount);
        Assert.Equal(wordIds.Count, await dbContext.UserWordProgresses.CountAsync(progress =>
            progress.UserId == user.Id && wordIds.Contains(progress.WordId)));

        var levelProgress = await dbContext.UserLevelProgresses.SingleAsync(progress =>
            progress.UserId == user.Id && progress.LevelId == level.Id);
        Assert.True(levelProgress.WordsLearned);
    }

    [Fact]
    public async Task MarkLevelWordsLearnedAsync_DoesNotDuplicateExistingLearnedWords()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "user@example.com");
        var level = await GetSectionLevelAsync(dbContext, "Food", 1);
        var wordIds = await GetActiveWordIdsAsync(dbContext, level.Id);
        var request = new MarkLevelWordsLearnedRequest
        {
            WordIds = [.. wordIds, wordIds[0], wordIds[1]]
        };
        var service = new WordLearningService(dbContext);

        await service.MarkLevelWordsLearnedAsync(user.Id, level.Id, request);
        var result = await service.MarkLevelWordsLearnedAsync(user.Id, level.Id, request);

        Assert.Equal(wordIds.Count, result.LearnedWordsCount);
        Assert.Equal(wordIds.Count, await dbContext.UserWordProgresses.CountAsync(progress =>
            progress.UserId == user.Id && wordIds.Contains(progress.WordId)));
    }

    [Fact]
    public async Task MarkLevelWordsLearnedAsync_RejectsWordsOutsideLevel()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "user@example.com");
        var level = await GetSectionLevelAsync(dbContext, "Food", 1);
        var otherLevel = await GetSectionLevelAsync(dbContext, "Food", 2);
        var wordIds = await GetActiveWordIdsAsync(dbContext, level.Id);
        var otherWordId = await dbContext.Words
            .Where(word => word.LevelId == otherLevel.Id && word.IsActive)
            .Select(word => word.Id)
            .FirstAsync();
        wordIds[0] = otherWordId;
        var service = new WordLearningService(dbContext);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.MarkLevelWordsLearnedAsync(
                user.Id,
                level.Id,
                new MarkLevelWordsLearnedRequest { WordIds = wordIds }));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Equal("validation_error", exception.Code);
        Assert.Equal(["Word ids must belong to the level"], exception.Details!["wordIds"]);
    }

    [Fact]
    public async Task MarkLevelWordsLearnedAsync_RejectsIncompleteWordList()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "user@example.com");
        var level = await GetSectionLevelAsync(dbContext, "Food", 1);
        var wordIds = await GetActiveWordIdsAsync(dbContext, level.Id);
        wordIds.RemoveAt(wordIds.Count - 1);
        var service = new WordLearningService(dbContext);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.MarkLevelWordsLearnedAsync(
                user.Id,
                level.Id,
                new MarkLevelWordsLearnedRequest { WordIds = wordIds }));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Equal("validation_error", exception.Code);
        Assert.Equal(["All active level words must be marked as learned"], exception.Details!["wordIds"]);
    }

    [Fact]
    public async Task MarkLevelWordsLearnedAsync_RejectsFinalTest()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "user@example.com");
        var finalTest = await GetSectionLevelAsync(dbContext, "Food", 6);
        await SetLevelStatusAsync(dbContext, user.Id, finalTest.Id, "available");
        var service = new WordLearningService(dbContext);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.MarkLevelWordsLearnedAsync(
                user.Id,
                finalTest.Id,
                new MarkLevelWordsLearnedRequest { WordIds = [1] }));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Equal("validation_error", exception.Code);
        Assert.Equal(["Final tests do not introduce new words"], exception.Details!["levelId"]);
    }

    [Fact]
    public async Task MarkLevelWordsLearnedAsync_UsesCurrentUserOnly()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "user@example.com");
        var otherUser = await CreateUserWithLevelProgressAsync(dbContext, "other@example.com");
        var level = await GetSectionLevelAsync(dbContext, "Food", 1);
        var wordIds = await GetActiveWordIdsAsync(dbContext, level.Id);
        var service = new WordLearningService(dbContext);

        await service.MarkLevelWordsLearnedAsync(
            user.Id,
            level.Id,
            new MarkLevelWordsLearnedRequest { WordIds = wordIds });

        Assert.Equal(wordIds.Count, await dbContext.UserWordProgresses.CountAsync(progress =>
            progress.UserId == user.Id));
        Assert.Equal(0, await dbContext.UserWordProgresses.CountAsync(progress =>
            progress.UserId == otherUser.Id));
    }

    private static async Task<User> CreateUserWithLevelProgressAsync(AppDbContext dbContext, string email)
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
            Name = "Vladimir",
            AvatarId = 1,
            CreatedAt = now
        });

        var levels = await dbContext.Levels
            .Include(level => level.Section)
            .Where(level => level.IsActive && level.Section.IsActive)
            .ToListAsync();

        dbContext.UserLevelProgresses.AddRange(levels.Select(level =>
        {
            var isFirstNormalLevel = !level.IsFinalTest
                && level.LevelNumber == levels
                    .Where(candidate => candidate.SectionId == level.SectionId && !candidate.IsFinalTest)
                    .Min(candidate => candidate.LevelNumber);

            return new UserLevelProgress
            {
                UserId = user.Id,
                LevelId = level.Id,
                Status = isFirstNormalLevel ? "available" : "locked",
                WordsLearned = false,
                AttemptsCount = 0,
                UnlockedAt = isFirstNormalLevel ? now : null
            };
        }));

        await dbContext.SaveChangesAsync();

        return user;
    }

    private static async Task SetLevelStatusAsync(
        AppDbContext dbContext,
        Guid userId,
        int levelId,
        string status)
    {
        var progress = await dbContext.UserLevelProgresses
            .SingleAsync(candidate => candidate.UserId == userId && candidate.LevelId == levelId);

        progress.Status = status;

        await dbContext.SaveChangesAsync();
    }

    private static async Task<Level> GetSectionLevelAsync(
        AppDbContext dbContext,
        string sectionTitle,
        int levelNumber) =>
        await dbContext.Levels
            .Include(level => level.Section)
            .SingleAsync(level => level.Section.Title == sectionTitle && level.LevelNumber == levelNumber);

    private static async Task<List<int>> GetActiveWordIdsAsync(AppDbContext dbContext, int levelId) =>
        await dbContext.Words
            .Where(word => word.LevelId == levelId && word.IsActive)
            .OrderBy(word => word.Id)
            .Select(word => word.Id)
            .ToListAsync();

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
