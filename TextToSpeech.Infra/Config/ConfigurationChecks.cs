using Microsoft.Extensions.Configuration;

namespace TextToSpeech.Infra.Config;

internal static class ConfigurationChecks
{
    public static void Validate(IConfiguration configuration)
    {
        if (HostingEnvironment.IsTestMode())
        {
            return;
        }

        ValidateRequiredValue(configuration[ConfigConstants.ElevenLabsApiKey], ConfigConstants.ElevenLabsApiKey);
        ValidateRequiredValue(configuration[ConfigConstants.OpenAiApiKey], ConfigConstants.OpenAiApiKey);

        var narakeet = configuration.GetSection(ConfigConstants.SectionNames.NarakeetConfig).Get<NarakeetConfig>()
            ?? throw new InvalidOperationException("Narakeet configuration is missing or invalid.");

        if (!Uri.TryCreate(narakeet.ApiUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                "Configuration value " +
                $"'{ConfigConstants.SectionNames.NarakeetConfig}:{nameof(NarakeetConfig.ApiUrl)}' " +
                "must be an absolute URL.");
        }

        ValidateRequiredValue(
            narakeet.ApiKey,
            $"{ConfigConstants.SectionNames.NarakeetConfig}:{nameof(NarakeetConfig.ApiKey)}");
    }

    private static void ValidateRequiredValue(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value '{name}' is required.");
        }
    }
}
