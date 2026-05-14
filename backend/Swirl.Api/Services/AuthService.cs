using Microsoft.EntityFrameworkCore;
using Swirl.Api.Data;
using Swirl.Api.Interfaces;
using Swirl.Api.Models;
using Swirl.Api.Requests;
using Swirl.Api.Responses;

namespace Swirl.Api.Services;

public class AuthService(
    AppDbContext dbContext,
    IPasswordHashService passwordHashService,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRegisterRequest(request);

        var now = CreateTimestamp();
        var email = NormalizeEmail(request.Email);

        var avatar = await dbContext.Avatars
            .Where(candidate => candidate.Id == request.AvatarId && candidate.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (avatar is null)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "validation_error",
                "Validation failed",
                new Dictionary<string, string[]>
                {
                    ["avatarId"] = ["Avatar must exist and be active"]
                });
        }

        var emailExists = await dbContext.Users
            .AnyAsync(user => user.Email == email, cancellationToken);

        if (emailExists)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "email_already_exists",
                "Email is already registered");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            CreatedAt = now
        };
        user.PasswordHash = passwordHashService.HashPassword(user, request.Password);

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = request.Name.Trim(),
            AvatarId = avatar.Id,
            CurrentStreak = 0,
            BestStreak = 0,
            CreatedAt = now
        };

        var levels = await dbContext.Levels
            .Include(level => level.Section)
            .Where(level => level.IsActive && level.Section.IsActive)
            .OrderBy(level => level.Section.SortOrder)
            .ThenBy(level => level.SortOrder)
            .ToListAsync(cancellationToken);

        var firstNormalLevelIds = levels
            .Where(level => !level.IsFinalTest)
            .GroupBy(level => level.SectionId)
            .Select(group => group.OrderBy(level => level.SortOrder).First().Id)
            .ToHashSet();

        var progress = levels.Select(level =>
        {
            var isAvailable = firstNormalLevelIds.Contains(level.Id);

            return new UserLevelProgress
            {
                UserId = user.Id,
                LevelId = level.Id,
                Status = isAvailable ? "available" : "locked",
                WordsLearned = false,
                UnlockedAt = isAvailable ? now : null,
                AttemptsCount = 0
            };
        });

        dbContext.Users.Add(user);
        dbContext.UserProfiles.Add(profile);
        dbContext.UserLevelProgresses.AddRange(progress);

        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user, profile, avatar);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateLoginRequest(request);

        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users
            .Include(candidate => candidate.UserProfile!)
            .ThenInclude(profile => profile.Avatar)
            .FirstOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null || !passwordHashService.VerifyPassword(user, request.Password))
        {
            throw new ApiException(
                StatusCodes.Status401Unauthorized,
                "invalid_credentials",
                "Invalid email or password");
        }

        return CreateAuthResponse(user, user.UserProfile!, user.UserProfile!.Avatar);
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(candidate => candidate.UserProfile!)
            .ThenInclude(profile => profile.Avatar)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        return user?.UserProfile is null
            ? null
            : CreateCurrentUserResponse(user, user.UserProfile, user.UserProfile.Avatar);
    }

    private AuthResponse CreateAuthResponse(User user, UserProfile profile, Avatar avatar) =>
        new()
        {
            AccessToken = jwtTokenService.CreateAccessToken(user),
            User = CreateCurrentUserResponse(user, profile, avatar)
        };

    private static CurrentUserResponse CreateCurrentUserResponse(
        User user,
        UserProfile profile,
        Avatar avatar) =>
        new()
        {
            Id = user.Id.ToString(),
            Name = profile.Name,
            Email = user.Email,
            AvatarUrl = avatar.ImageUrl
        };

    private static void ValidateRegisterRequest(RegisterRequest request)
    {
        var details = new Dictionary<string, string[]>();

        AddRequiredError(details, "name", request.Name, "Name is required");
        AddRequiredError(details, "email", request.Email, "Email is required");
        AddRequiredError(details, "password", request.Password, "Password is required");
        AddRequiredError(details, "confirmPassword", request.ConfirmPassword, "Confirm password is required");

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            details["confirmPassword"] = ["Password and confirmPassword must match"];
        }

        if (request.AvatarId <= 0)
        {
            details["avatarId"] = ["Avatar is required"];
        }

        ThrowIfValidationFailed(details);
    }

    private static void ValidateLoginRequest(LoginRequest request)
    {
        var details = new Dictionary<string, string[]>();

        AddRequiredError(details, "email", request.Email, "Email is required");
        AddRequiredError(details, "password", request.Password, "Password is required");

        ThrowIfValidationFailed(details);
    }

    private static void AddRequiredError(
        IDictionary<string, string[]> details,
        string key,
        string? value,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            details[key] = [message];
        }
    }

    private static void ThrowIfValidationFailed(IDictionary<string, string[]> details)
    {
        if (details.Count == 0)
        {
            return;
        }

        throw new ApiException(
            StatusCodes.Status400BadRequest,
            "validation_error",
            "Validation failed",
            details);
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static DateTime CreateTimestamp() =>
        DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}
