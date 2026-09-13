
namespace TextToSpeech.Infra.Constants;

public static class Shared
{
    public static class OpenAI
    {
        public const string Key = "openai";
        public const string Name = "OpenAI";
        public static readonly Guid Id = Guid.Parse("6ce9e704-e049-4128-b7c6-97fe5d1716fd");
    }

    public static class Narakeet
    {
        public const string Key = "narakeet";
        public const string Name = "Narakeet";
        public static readonly Guid Id = Guid.Parse("985e59c1-f5d2-4a19-853f-c7ee994ed34b");
    }

    public static class ElevenLabs
    {
        public const string Key = "elevenlabs";
        public const string Name = "ElevenLabs";
        public static readonly Guid Id = Guid.Parse("70725f60-b457-4519-a1bc-6fb8212eb154");
    }

    public static readonly IReadOnlyDictionary<string, Guid> TtsApis = new Dictionary<string, Guid>()
    {
        [Narakeet.Key] = Narakeet.Id,
        [OpenAI.Key] = OpenAI.Id,
        [ElevenLabs.Key] = ElevenLabs.Id,
    };

    public const string AudioStatusUpdated = "AudioStatusUpdated";
    public const string AudioHubEndpoint = "/audioHub";
}
