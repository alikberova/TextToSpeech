using TextToSpeech.Infra.Dto;

namespace TextToSpeech.Infra.Interfaces;

public interface IEmailService
{
    void SendEmail(EmailRequest request);
}
