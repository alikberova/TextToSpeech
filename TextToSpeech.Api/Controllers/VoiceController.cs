using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TextToSpeech.Api.Services;
using TextToSpeech.Core.Interfaces;

namespace TextToSpeech.Api.Controllers;

[Authorize]
[Route("api/voices")]
[ApiController]
public class VoiceController(IVoiceService voiceService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string provider)
    {
        var result = await voiceService.GetVoices(provider);

        if (result is null)
        {
            return NotFound();
        }

        // for 2 weeks
        HttpHeaderHelper.SetCacheControl(Response, 1209600);

        return Ok(result);
    }
}
