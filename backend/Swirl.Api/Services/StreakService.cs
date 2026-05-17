using Microsoft.EntityFrameworkCore;
using Swirl.Api.Data;
using Swirl.Api.Interfaces;
using Swirl.Api.Responses;

namespace Swirl.Api.Services;

public class StreakService : IStreakService
{
    private readonly AppDbContext _context;

    public StreakService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StreakResponse> UpdateLearningActivityAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (profile is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "not_found",
                "Resource not found");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        if (profile.LastActivityDate is null)
        {
            profile.CurrentStreak = 1;
            profile.LastActivityDate = today;
        }
        else if (profile.LastActivityDate == today.AddDays(-1))
        {
            profile.CurrentStreak += 1;
            profile.LastActivityDate = today;
        }
        else if (profile.LastActivityDate < today.AddDays(-1))
        {
            profile.CurrentStreak = 1;
            profile.LastActivityDate = today;
        }

        profile.BestStreak = Math.Max(profile.BestStreak, profile.CurrentStreak);
        profile.UpdatedAt = CreateTimestamp();

        await _context.SaveChangesAsync(cancellationToken);

        return new StreakResponse
        {
            CurrentStreak = profile.CurrentStreak,
            BestStreak = profile.BestStreak
        };
    }

    private static DateTime CreateTimestamp()
    {
        return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
    }
}
