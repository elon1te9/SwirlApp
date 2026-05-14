using Microsoft.EntityFrameworkCore;
using Swirl.Api.Data;
using Swirl.Api.Models;
using Swirl.Api.Services;

namespace Swirl.Api.Tests;

public class ContentServiceTests
{
    [Fact]
    public async Task GetSectionsAsync_ReturnsActiveSectionsWithCurrentUserProgress()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "first@example.com");
        var otherUser = await CreateUserWithLevelProgressAsync(dbContext, "second@example.com");
        var foodLevels = await GetSectionLevelsAsync(dbContext, "Food");
        var science = await dbContext.Sections.SingleAsync(section => section.Title == "Science");
        science.IsActive = false;

        await CompleteLevelAsync(dbContext, user.Id, foodLevels[0].Id);
        await CompleteLevelAsync(dbContext, otherUser.Id, foodLevels[1].Id);
        await dbContext.SaveChangesAsync();

        var service = new ContentService(dbContext);

        var result = await service.GetSectionsAsync(user.Id);

        Assert.Equal(["Food", "Health", "Wardrobe"], result.Select(section => section.Title).ToArray());

        var food = Assert.Single(result, section => section.Title == "Food");
        Assert.Equal(foodLevels[0].SectionId, food.Id);
        Assert.Equal("Words about food and drinks", food.Description);
        Assert.Equal("/media/images/sections/food.png", food.ImageUrl);
        Assert.Equal(17, food.ProgressPercent);
        Assert.Equal(1, food.CompletedLevels);
        Assert.Equal(6, food.TotalLevels);
    }

    [Fact]
    public async Task GetSectionAsync_ReturnsNullForMissingOrInactiveSection()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "user@example.com");
        var food = await dbContext.Sections.SingleAsync(section => section.Title == "Food");
        food.IsActive = false;
        await dbContext.SaveChangesAsync();

        var service = new ContentService(dbContext);

        Assert.Null(await service.GetSectionAsync(user.Id, food.Id));
        Assert.Null(await service.GetSectionAsync(user.Id, 9999));
    }

    [Fact]
    public async Task GetSectionLevelsAsync_ReturnsActiveLevelsWithCountsAndUserStatuses()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "first@example.com");
        var otherUser = await CreateUserWithLevelProgressAsync(dbContext, "second@example.com");
        var foodLevels = await GetSectionLevelsAsync(dbContext, "Food");
        foodLevels[2].IsActive = false;
        await CompleteLevelAsync(dbContext, user.Id, foodLevels[0].Id);
        await SetLevelStatusAsync(dbContext, user.Id, foodLevels[1].Id, "available");
        await CompleteLevelAsync(dbContext, otherUser.Id, foodLevels[1].Id);
        await AddWordAndExerciseAsync(dbContext, foodLevels[0].Id);
        await dbContext.SaveChangesAsync();

        var service = new ContentService(dbContext);

        var result = await service.GetSectionLevelsAsync(user.Id, foodLevels[0].SectionId);

        Assert.NotNull(result);
        Assert.Equal([1, 2, 4, 5, 6], result.Select(level => level.LevelNumber).ToArray());

        var firstLevel = result[0];
        var expectedFirstLevelWordsCount = await CountActiveWordsAsync(dbContext, foodLevels[0].Id);
        var expectedFirstLevelExercisesCount = await CountActiveExercisesAsync(dbContext, foodLevels[0].Id);
        Assert.Equal(foodLevels[0].Id, firstLevel.Id);
        Assert.Equal(foodLevels[0].SectionId, firstLevel.SectionId);
        Assert.Equal("Food Level 1", firstLevel.Title);
        Assert.Equal("A1", firstLevel.CefrLevel);
        Assert.Equal("Level 1 for Food section", firstLevel.Description);
        Assert.Equal(expectedFirstLevelWordsCount, firstLevel.WordsCount);
        Assert.Equal(expectedFirstLevelExercisesCount, firstLevel.ExercisesCount);
        Assert.False(firstLevel.IsFinalTest);
        Assert.Equal("completed", firstLevel.Status);

        Assert.Equal("available", result.Single(level => level.LevelNumber == 2).Status);
        Assert.Equal("locked", result.Single(level => level.LevelNumber == 6).Status);
        Assert.True(result.Single(level => level.LevelNumber == 6).IsFinalTest);
    }

    [Fact]
    public async Task GetSectionLevelsAsync_UsesFallbackStatusesWhenProgressIsMissing()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "user@example.com");
        var foodLevels = await GetSectionLevelsAsync(dbContext, "Food");
        var progresses = await dbContext.UserLevelProgresses
            .Where(progress => progress.UserId == user.Id && foodLevels.Select(level => level.Id).Contains(progress.LevelId))
            .ToListAsync();
        dbContext.UserLevelProgresses.RemoveRange(progresses);
        await dbContext.SaveChangesAsync();

        var service = new ContentService(dbContext);

        var result = await service.GetSectionLevelsAsync(user.Id, foodLevels[0].SectionId);

        Assert.NotNull(result);
        Assert.Equal("available", result.Single(level => level.LevelNumber == 1).Status);
        Assert.All(result.Where(level => level.LevelNumber != 1), level => Assert.Equal("locked", level.Status));
    }

    [Fact]
    public async Task GetLevelAsync_ReturnsDetailsWithSectionTitleStatusAndWordsLearned()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "user@example.com");
        var foodLevels = await GetSectionLevelsAsync(dbContext, "Food");
        var secondLevel = foodLevels[1];
        await SetLevelStatusAsync(dbContext, user.Id, secondLevel.Id, "available", wordsLearned: true);
        await AddWordAndExerciseAsync(dbContext, secondLevel.Id);
        await dbContext.SaveChangesAsync();

        var service = new ContentService(dbContext);

        var result = await service.GetLevelAsync(user.Id, secondLevel.Id);

        Assert.NotNull(result);
        Assert.Equal(secondLevel.Id, result.Id);
        Assert.Equal(secondLevel.SectionId, result.SectionId);
        Assert.Equal("Food", result.SectionTitle);
        Assert.Equal("Food Level 2", result.Title);
        Assert.Equal(2, result.LevelNumber);
        Assert.Equal("A1", result.CefrLevel);
        Assert.Equal("Level 2 for Food section", result.Description);
        Assert.Equal(await CountActiveWordsAsync(dbContext, secondLevel.Id), result.WordsCount);
        Assert.Equal(await CountActiveExercisesAsync(dbContext, secondLevel.Id), result.ExercisesCount);
        Assert.False(result.IsFinalTest);
        Assert.Equal("available", result.Status);
        Assert.True(result.WordsLearned);
    }

    [Fact]
    public async Task GetLevelAsync_ReturnsNullForInactiveLevelOrInactiveSection()
    {
        await using var dbContext = await CreateSeededDbContextAsync();
        var user = await CreateUserWithLevelProgressAsync(dbContext, "user@example.com");
        var foodLevels = await GetSectionLevelsAsync(dbContext, "Food");
        var scienceLevels = await GetSectionLevelsAsync(dbContext, "Science");
        var science = await dbContext.Sections.SingleAsync(section => section.Title == "Science");
        foodLevels[0].IsActive = false;
        science.IsActive = false;
        await dbContext.SaveChangesAsync();

        var service = new ContentService(dbContext);

        Assert.Null(await service.GetLevelAsync(user.Id, foodLevels[0].Id));
        Assert.Null(await service.GetLevelAsync(user.Id, scienceLevels[0].Id));
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

    private static async Task CompleteLevelAsync(AppDbContext dbContext, Guid userId, int levelId) =>
        await SetLevelStatusAsync(dbContext, userId, levelId, "completed");

    private static async Task SetLevelStatusAsync(
        AppDbContext dbContext,
        Guid userId,
        int levelId,
        string status,
        bool wordsLearned = false)
    {
        var progress = await dbContext.UserLevelProgresses
            .SingleAsync(candidate => candidate.UserId == userId && candidate.LevelId == levelId);

        progress.Status = status;
        progress.WordsLearned = wordsLearned;
    }

    private static async Task AddWordAndExerciseAsync(AppDbContext dbContext, int levelId)
    {
        var now = CreateTimestamp();
        var word = new Word
        {
            LevelId = levelId,
            English = $"word-{levelId}",
            Russian = $"word-{levelId}",
            CefrLevel = "A1",
            IsActive = true,
            CreatedAt = now
        };

        dbContext.Words.Add(word);
        await dbContext.SaveChangesAsync();

        dbContext.Exercises.Add(new Exercise
        {
            LevelId = levelId,
            WordId = word.Id,
            Type = "english_to_russian_choice",
            QuestionText = word.English,
            CorrectAnswer = word.Russian,
            IsActive = true,
            CreatedAt = now
        });
    }

    private static async Task<List<Level>> GetSectionLevelsAsync(AppDbContext dbContext, string sectionTitle) =>
        await dbContext.Levels
            .Include(level => level.Section)
            .Where(level => level.Section.Title == sectionTitle)
            .OrderBy(level => level.SortOrder)
            .ToListAsync();

    private static async Task<int> CountActiveWordsAsync(AppDbContext dbContext, int levelId) =>
        await dbContext.Words.CountAsync(word => word.LevelId == levelId && word.IsActive);

    private static async Task<int> CountActiveExercisesAsync(AppDbContext dbContext, int levelId) =>
        await dbContext.Exercises.CountAsync(exercise => exercise.LevelId == levelId && exercise.IsActive);

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
