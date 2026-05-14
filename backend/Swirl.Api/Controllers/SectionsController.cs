using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swirl.Api.Interfaces;
using Swirl.Api.Responses;

namespace Swirl.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sections")]
public class SectionsController(IContentService contentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SectionResponse>>> GetSections(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return CreateUnauthorizedResult();
        }

        return Ok(await contentService.GetSectionsAsync(userId.Value, cancellationToken));
    }

    [HttpGet("{sectionId:int}")]
    public async Task<ActionResult<SectionResponse>> GetSection(
        int sectionId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return CreateUnauthorizedResult();
        }

        var section = await contentService.GetSectionAsync(userId.Value, sectionId, cancellationToken);

        return section is null
            ? CreateNotFoundResult()
            : Ok(section);
    }

    [HttpGet("{sectionId:int}/levels")]
    public async Task<ActionResult<List<LevelResponse>>> GetSectionLevels(
        int sectionId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return CreateUnauthorizedResult();
        }

        var levels = await contentService.GetSectionLevelsAsync(userId.Value, sectionId, cancellationToken);

        return levels is null
            ? CreateNotFoundResult()
            : Ok(levels);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdValue, out var userId)
            ? userId
            : null;
    }

    private static UnauthorizedObjectResult CreateUnauthorizedResult() =>
        new(new ErrorResponse(new ErrorDetails(
            "unauthorized",
            "Authentication is required")));

    private static NotFoundObjectResult CreateNotFoundResult() =>
        new(new ErrorResponse(new ErrorDetails(
            "not_found",
            "Resource not found")));
}
