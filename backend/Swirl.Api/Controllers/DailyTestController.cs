using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swirl.Api.Interfaces;
using Swirl.Api.Requests;
using Swirl.Api.Responses;

namespace Swirl.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/daily-test")]
public class DailyTestController : ControllerBase
{
    private readonly IDailyTestService _dailyTestService;

    public DailyTestController(IDailyTestService dailyTestService)
    {
        _dailyTestService = dailyTestService;
    }

    [HttpGet]
    public async Task<ActionResult<DailyTestResponse>> GetDailyTest(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return CreateUnauthorizedResult();
        }

        var dailyTest = await _dailyTestService.GetDailyTestAsync(userId.Value, cancellationToken);
        return Ok(dailyTest);
    }

    [HttpPost("complete")]
    public async Task<ActionResult<CompleteDailyTestResponse>> CompleteDailyTest(
        CompleteDailyTestRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return CreateUnauthorizedResult();
        }

        var result = await _dailyTestService.CompleteDailyTestAsync(
            userId.Value,
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
