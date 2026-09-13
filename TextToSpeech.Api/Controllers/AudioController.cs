using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Repositories;

namespace TextToSpeech.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public sealed class AudioController : ControllerBase
{
    private readonly IAudioFileRepository _audioFileRepository;
    private readonly IOwnerContext _ownerContext;

    public AudioController(IAudioFileRepository audioFileRepository, IOwnerContext ownerContext)
    {
        _audioFileRepository = audioFileRepository;
        _ownerContext = ownerContext;
    }

    [HttpGet("download/{fileId}")]
    public async Task<IActionResult> Download(string fileId)
    {
        if (!Guid.TryParse(fileId, out Guid parsedFileId))
        {
            return BadRequest("Invalid file ID.");
        }

        var dbAudioFile = await _audioFileRepository.GetById(parsedFileId);

        if (dbAudioFile is null)
        {
            return NotFound("File not found.");
        }

        var ownerId = _ownerContext.GetOwnerId();

        if (dbAudioFile.OwnerId != ownerId)
        {
            return Forbid();
        }

        return File(dbAudioFile.Data, "audio/mpeg");
    }
}