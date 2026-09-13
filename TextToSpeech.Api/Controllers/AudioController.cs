using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Repositories;

namespace TextToSpeech.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public sealed class AudioController(
    IAudioFileRepository audioFileRepository,
    IOwnerContext ownerContext,
    IArtifactStorage storage) : ControllerBase
{
    [HttpGet("download/{fileId}")]
    public async Task<IActionResult> Download(string fileId)
    {
        if (!Guid.TryParse(fileId, out Guid parsedFileId))
        {
            return BadRequest("Invalid file ID.");
        }

        var dbAudioFile = await audioFileRepository.GetById(parsedFileId);

        if (dbAudioFile is null)
        {
            return NotFound("File not found.");
        }

        var ownerId = ownerContext.GetOwnerId();

        if (dbAudioFile.OwnerId != ownerId)
        {
            return Forbid();
        }

        var content = await storage.OpenReadAsync(dbAudioFile.ContentId, HttpContext.RequestAborted);

        return File(content, "audio/mpeg");
    }
}
