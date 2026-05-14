using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swirl.Api.Interfaces;
using Swirl.Api.Responses;

namespace Swirl.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/levels")]
public class LevelsController(IContentService contentService) : ControllerBase
{
    [HttpGet("{levelId:int}")]
    public async Task<ActionResult<LevelDetailsResponse>> GetLevel(
        int levelId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ErrorResponse(new ErrorDetails(
                "unauthorized",
                "Authentication is required")));
        }

        var level = await contentService.GetLevelAsync(userId.Value, levelId, cancellationToken);

        return level is null
            ? NotFound(new ErrorResponse(new ErrorDetails(
                "not_found",
                "Resource not found")))
            : Ok(level);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdValue, out var userId)
            ? userId
            : null;
    }
}
