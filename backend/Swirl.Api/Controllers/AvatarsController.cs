using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swirl.Api.Interfaces;
using Swirl.Api.Responses;

namespace Swirl.Api.Controllers;

[ApiController]
[Route("api/avatars")]
public class AvatarsController(IProfileService profileService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<AvatarResponse>>> GetAvatars(CancellationToken cancellationToken) =>
        Ok(await profileService.GetAvatarsAsync(cancellationToken));
}
