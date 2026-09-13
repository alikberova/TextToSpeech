using Microsoft.AspNetCore.Mvc;
using TextToSpeech.Infra.Dto;
using TextToSpeech.Infra.Interfaces;

namespace TextToSpeech.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class EmailController(IEmailService emailService) : ControllerBase
{
    [HttpPost]
    public void Send([FromBody] EmailRequest request)
    {
        emailService.SendEmail(request);
    }
}