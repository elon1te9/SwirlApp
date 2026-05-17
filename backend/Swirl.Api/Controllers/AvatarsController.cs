using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swirl.Api.Interfaces;
using Swirl.Api.Responses;

namespace Swirl.Api.Controllers;

[ApiController]
[Route("api/avatars")]
public class AvatarsController : ControllerBase
{
    private readonly IProfileService _profileService;

    public AvatarsController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<AvatarResponse>>> GetAvatars(CancellationToken cancellationToken)
    {
        var avatars = await _profileService.GetAvatarsAsync(cancellationToken);
        return Ok(avatars);
    }
}
