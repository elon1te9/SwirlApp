using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swirl.Api.Interfaces;
using Swirl.Api.Responses;

namespace Swirl.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sections")]
public class SectionsController : ControllerBase
{
    private readonly IContentService _contentService;

    public SectionsController(IContentService contentService)
    {
        _contentService = contentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<SectionResponse>>> GetSections(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return CreateUnauthorizedResult();
        }

        var sections = await _contentService.GetSectionsAsync(userId.Value, cancellationToken);
        return Ok(sections);
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

        var section = await _contentService.GetSectionAsync(userId.Value, sectionId, cancellationToken);

        if (section is null)
        {
            return CreateNotFoundResult();
        }

        return Ok(section);
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

        var levels = await _contentService.GetSectionLevelsAsync(userId.Value, sectionId, cancellationToken);

        if (levels is null)
        {
            return CreateNotFoundResult();
        }

        return Ok(levels);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return userId;
    }

    private static UnauthorizedObjectResult CreateUnauthorizedResult()
    {
        return new UnauthorizedObjectResult(new ErrorResponse(new ErrorDetails(
            "unauthorized",
            "Authentication is required")));
    }

    private static NotFoundObjectResult CreateNotFoundResult()
    {
        return new NotFoundObjectResult(new ErrorResponse(new ErrorDetails(
            "not_found",
            "Resource not found")));
    }
}
