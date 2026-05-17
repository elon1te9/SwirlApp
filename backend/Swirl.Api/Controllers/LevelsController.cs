using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swirl.Api.Interfaces;
using Swirl.Api.Requests;
using Swirl.Api.Responses;

namespace Swirl.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/levels")]
public class LevelsController : ControllerBase
{
    private readonly IContentService _contentService;
    private readonly IWordLearningService _wordLearningService;
    private readonly ILearningService _learningService;

    public LevelsController(
        IContentService contentService,
        IWordLearningService wordLearningService,
        ILearningService learningService)
    {
        _contentService = contentService;
        _wordLearningService = wordLearningService;
        _learningService = learningService;
    }

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

        var level = await _contentService.GetLevelAsync(userId.Value, levelId, cancellationToken);

        if (level is null)
        {
            return NotFound(new ErrorResponse(new ErrorDetails(
                "not_found",
                "Resource not found")));
        }

        return Ok(level);
    }

    [HttpGet("{levelId:int}/words")]
    public async Task<ActionResult<List<WordResponse>>> GetLevelWords(
        int levelId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return CreateUnauthorizedResult();
        }

        var words = await _wordLearningService.GetLevelWordsAsync(
            userId.Value,
            levelId,
            cancellationToken);

        return Ok(words);
    }

    [HttpPost("{levelId:int}/words/mark-learned")]
    public async Task<ActionResult<MarkLevelWordsLearnedResponse>> MarkLevelWordsLearned(
        int levelId,
        MarkLevelWordsLearnedRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return CreateUnauthorizedResult();
        }

        var result = await _wordLearningService.MarkLevelWordsLearnedAsync(
            userId.Value,
            levelId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{levelId:int}/session")]
    public async Task<ActionResult<LevelSessionResponse>> GetLevelSession(
        int levelId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return CreateUnauthorizedResult();
        }

        var session = await _learningService.GetLevelSessionAsync(
            userId.Value,
            levelId,
            cancellationToken);

        return Ok(session);
    }

    [HttpPost("{levelId:int}/complete")]
    public async Task<ActionResult<CompleteLevelResponse>> CompleteLevel(
        int levelId,
        CompleteLevelRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return CreateUnauthorizedResult();
        }

        var result = await _learningService.CompleteLevelAsync(
            userId.Value,
            levelId,
            request,
            cancellationToken);

        return Ok(result);
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
}
