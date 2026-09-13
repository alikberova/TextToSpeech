namespace TextToSpeech.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TextToSpeech.Infra.Interfaces;

[Authorize]
[ApiController]
[Route("test")]
public sealed class TestSeedController(
    ITestSeedService seedService) : ControllerBase
{
    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        if (!Infra.HostingEnvironment.IsTestMode())
        {
            return NotFound();
        }

        await seedService.SeedAsync();

        return Ok();
    }
}
