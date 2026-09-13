using System.Security.Cryptography;
using System.Text;
using TextToSpeech.Core.Entities;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Constants;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Infra.Services;

public static class AudioFileBuilder
{
    public static AudioFile Create(byte[] bytes,
        AudioType type,
        string input,
        TtsRequestOptions options,
        string ownerId,
        Guid? ttsApiId = null,
        string? fileName = null,
        Guid? id = null)
    {
        var langCode = options.Voice.Language?.LanguageCode;

        var audio = new AudioFile
        {
            Id = id ?? Guid.NewGuid(),
            FileName = fileName ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            Description = $"{type}_" +
                $"{options.Voice.Name}-{options.Voice.ProviderVoiceId}_" +
                $"{langCode}_" +
                $"{Shared.TtsApis.FirstOrDefault(kv => kv.Value == ttsApiId).Key}_" +
                $"{options.Speed}_" +
                $"{options.Model}",
            Hash = GenerateHash(input, options, type),
            Voice = options.Voice.ProviderVoiceId,
            LanguageCode = langCode,
            Speed = options.Speed,
            Type = type,
            TtsApiId = ttsApiId,
            OwnerId = ownerId
        };

        audio.SetDataOnce(bytes);

        return audio;
    }

    public static string GenerateHash(string input, TtsRequestOptions options, AudioType type)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(input);

        var dataHash = Convert.ToBase64String(SHA256.HashData(bytes));

        var details = $"{dataHash}" +
            $":{options.Voice.ProviderVoiceId}" +
            $":{options.Voice.Language?.LanguageCode}" +
            $":{options.Speed}" +
            $":{options.Model}" +
            $":{options.ResponseFormat}" +
            $"{type}";

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(details));

        return Convert.ToBase64String(hashBytes);
    }
}
