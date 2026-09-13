namespace TextToSpeech.Infra.Constants;

public static class CacheKeys
{
    public static string Voices(string provider) => $"voices:{provider}";
}
