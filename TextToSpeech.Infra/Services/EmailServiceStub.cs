using TextToSpeech.Infra.Dto;
using TextToSpeech.Infra.Interfaces;

namespace TextToSpeech.Infra.Services;

public class EmailServiceStub : IEmailService
{
    public void SendEmail(EmailRequest request)
    {
    }
}
