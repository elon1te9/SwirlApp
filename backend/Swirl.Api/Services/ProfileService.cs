using Microsoft.EntityFrameworkCore;
using Swirl.Api.Data;
using Swirl.Api.Interfaces;
using Swirl.Api.Requests;
using Swirl.Api.Responses;

namespace Swirl.Api.Services;

public class ProfileService(AppDbContext dbContext) : IProfileService
{
    public async Task<List<AvatarResponse>> GetAvatarsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Avatars
            .Where(avatar => avatar.IsActive)
            .OrderBy(avatar => avatar.Id)
            .Select(avatar => new AvatarResponse
            {
                Id = avatar.Id,
                Name = avatar.Name,
                ImageUrl = avatar.ImageUrl
            })
            .ToListAsync(cancellationToken);

    public async Task<ProfileResponse?> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.UserProfiles
            .Include(candidate => candidate.Avatar)
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var learnedWordsCount = await dbContext.UserWordProgresses
            .CountAsync(progress => progress.UserId == userId, cancellationToken);

        var completedLevelsCount = await dbContext.UserLevelProgresses
            .CountAsync(
                progress => progress.UserId == userId && progress.Status == "completed",
                cancellationToken);

        var completedLevelIds = await dbContext.UserLevelProgresses
            .Where(progress => progress.UserId == userId && progress.Status == "completed")
            .Select(progress => progress.LevelId)
            .ToListAsync(cancellationToken);

        var completedLevelIdsSet = completedLevelIds.ToHashSet();
        var sections = await dbContext.Sections
            .Include(section => section.Levels)
            .Where(section => section.IsActive)
            .OrderBy(section => section.SortOrder)
            .ToListAsync(cancellationToken);

        return new ProfileResponse
        {
            Name = profile.Name,
            AvatarUrl = profile.Avatar.ImageUrl,
            CurrentStreak = profile.CurrentStreak,
            BestStreak = profile.BestStreak,
            LearnedWordsCount = learnedWordsCount,
            CompletedLevelsCount = completedLevelsCount,
            SectionsProgress = sections.Select(section =>
            {
                var activeLevels = section.Levels
                    .Where(level => level.IsActive)
                    .ToList();
                var totalLevels = activeLevels.Count;
                var completedLevels = activeLevels.Count(level => completedLevelIdsSet.Contains(level.Id));

                return new SectionProgressResponse
                {
                    SectionId = section.Id,
                    Title = section.Title,
                    ProgressPercent = totalLevels == 0
                        ? 0
                        : (int)Math.Round(completedLevels * 100.0 / totalLevels)
                };
            }).ToList()
        };
    }

    public async Task<ChangeAvatarResponse> ChangeAvatarAsync(
        Guid userId,
        ChangeAvatarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AvatarId <= 0)
        {
            throw CreateInvalidAvatarException("Avatar is required");
        }

        var avatar = await dbContext.Avatars
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.AvatarId && candidate.IsActive,
                cancellationToken);

        if (avatar is null)
        {
            throw CreateInvalidAvatarException("Avatar must exist and be active");
        }

        var profile = await dbContext.UserProfiles
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (profile is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "not_found",
                "Resource not found");
        }

        profile.AvatarId = avatar.Id;
        profile.UpdatedAt = CreateTimestamp();

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ChangeAvatarResponse
        {
            AvatarUrl = avatar.ImageUrl
        };
    }

    private static ApiException CreateInvalidAvatarException(string message) =>
        new(
            StatusCodes.Status400BadRequest,
            "validation_error",
            "Validation failed",
            new Dictionary<string, string[]>
            {
                ["avatarId"] = [message]
            });

    private static DateTime CreateTimestamp() =>
        DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}
