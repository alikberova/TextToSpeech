using Microsoft.AspNetCore.Mvc;
using TextToSpeech.Api.Services;

namespace TextToSpeech.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class AuthController(IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("guest")]
    public IActionResult CreateGuestToken()
    {
        (var expires, var jwtString) = jwtTokenService.CreateGuestToken();

        return Ok(new
        {
            accessToken = jwtString,
            expiresAtUtc = expires
        });
    }
}
