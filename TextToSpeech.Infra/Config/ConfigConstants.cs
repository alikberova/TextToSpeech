namespace TextToSpeech.Infra.Config;

public static class ConfigConstants
{
    public static class SectionNames
    {
        public const string EmailConfig = "EmailConfig";
        public const string NarakeetConfig = "NarakeetConfig";
        public const string JwtConfig = "JwtConfig";
    }

    public static class ConnectionStrings
    {
        public static string CacheConnection => HostingEnvironment.IsTestMode() && HostingEnvironment.IsWindows()
            ? "RedisTestConnection"
            : "RedisConnection";
        public static string DbConnection => HostingEnvironment.IsTestMode() && HostingEnvironment.IsWindows()
            ? "DbTestConnection"
            : "DefaultConnection";
    }

    public const string AppDataPath = "AppDataPath";
    public const string OpenAiApiKey = "OPENAI_API_KEY";
    public const string ElevenLabsApiKey = "ELEVENLABS_API_KEY";
    public const string IsTestMode = "IsTestMode";
}
