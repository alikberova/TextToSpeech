using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TextToSpeech.Core.Interfaces;

namespace TextToSpeech.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public sealed class SpeechController : ControllerBase
{
    private readonly ISpeechService _speechService;
    private readonly ISubmitSpeechGeneration _submitSpeechGeneration;
    private readonly IOwnerContext _ownerContext;

    public SpeechController(ISpeechService speechService, IOwnerContext ownerContext,
        ISubmitSpeechGeneration submitSpeechGeneration)
    {
        _speechService = speechService;
        _ownerContext = ownerContext;
        _submitSpeechGeneration = submitSpeechGeneration;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSpeech([FromForm] TtsRequest request)
    {
        if (request.File is null)
        {
            return BadRequest("File is required for speech generation.");
        }

        using var ms = new MemoryStream();

        await request.File.CopyToAsync(ms, HttpContext.RequestAborted);

        var fileId = await _submitSpeechGeneration.SubmitAsync(
            request.TtsRequestOptions,
            ms.ToArray(),
            request.File.FileName,
            request.TtsApi,
            _ownerContext.GetOwnerId(),
            HttpContext.RequestAborted);

        return Ok(fileId);
    }

    [HttpPost("sample")]
    public async Task<IActionResult> GetSpeechSample([FromBody] TtsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            return BadRequest("Input text is required for speech sample.");
        }

        var audioStream = await _speechService.CreateSpeechSample(
            request.TtsRequestOptions,
            request.Input,
            request.TtsApi,
            _ownerContext.GetOwnerId());

        return new FileStreamResult(audioStream, "audio/mpeg");
    }
}
