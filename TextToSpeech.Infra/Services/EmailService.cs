using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using TextToSpeech.Core;
using TextToSpeech.Infra.Config;
using TextToSpeech.Infra.Dto;
using TextToSpeech.Infra.Interfaces;

namespace TextToSpeech.Infra.Services;

public sealed class EmailService : IEmailService
{
    private readonly EmailConfig _emailConfig;
    private readonly ISmtpClient _smtpClient;

    public EmailService(IOptions<EmailConfig> emailConfig, ISmtpClient smtpClient)
    {
        _emailConfig = emailConfig.Value;
        _smtpClient = smtpClient;
    }

    public void SendEmail(EmailRequest request)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(request.Name, _emailConfig.EmailFrom));
        email.To.Add(new MailboxAddress("Admin", _emailConfig.EmailTo));

        email.Subject = AppConstants.AppName;
        email.Body = new TextPart(MimeKit.Text.TextFormat.Text)
        {
            Text = $"Message from {request.UserEmail}:\n{request.Message}"
        };

        _smtpClient.Connect("smtp.gmail.com", 587, false);
        _smtpClient.Authenticate(_emailConfig.EmailFrom, _emailConfig.EmailFromPassword);
        _smtpClient.Send(email);
        _smtpClient.Disconnect(true);
    }
}