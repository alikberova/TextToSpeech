using TextToSpeech.Infra;
using TextToSpeech.Infra.Config;
using static TextToSpeech.Infra.Config.ConfigConstants;

namespace TextToSpeech.Api.Extensions;

internal static class ConfigurationChecks
{
    public static void Validate(this IConfiguration configuration)
    {
        ValidateRequiredValue(configuration[AppDataPath], AppDataPath);
        ValidateRequiredConnectionString(configuration, ConnectionStrings.DbConnection);
        ValidateRequiredConnectionString(configuration, ConnectionStrings.CacheConnection);
        ValidateJwtConfiguration(configuration);

        if (!HostingEnvironment.IsTestMode())
        {
            ValidateEmailConfiguration(configuration);
        }

        if (!HostingEnvironment.IsDevelopment())
        {
            ValidateElasticsearchConfiguration(configuration);
        }
    }

    private static void ValidateRequiredConnectionString(IConfiguration configuration, string name)
    {
        var connectionString = configuration.GetConnectionString(name);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{name}' is required.");
        }
    }

    private static void ValidateEmailConfiguration(IConfiguration configuration)
    {
        var email = configuration.GetSection(SectionNames.EmailConfig).Get<EmailConfig>()
            ?? throw new InvalidOperationException("Email configuration is missing or invalid.");

        ValidateRequiredValue(email.EmailTo, $"{SectionNames.EmailConfig}:{nameof(EmailConfig.EmailTo)}");
        ValidateRequiredValue(email.EmailFrom, $"{SectionNames.EmailConfig}:{nameof(EmailConfig.EmailFrom)}");
        ValidateRequiredValue(
            email.EmailFromPassword,
            $"{SectionNames.EmailConfig}:{nameof(EmailConfig.EmailFromPassword)}");
    }

    private static void ValidateElasticsearchConfiguration(IConfiguration configuration)
    {
        var elasticsearch = configuration.GetSection(nameof(ElasticsearchConfig)).Get<ElasticsearchConfig>()
            ?? throw new InvalidOperationException("Elasticsearch configuration is missing or invalid.");

        if (!Uri.TryCreate(elasticsearch.Url, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                $"Configuration value '{nameof(ElasticsearchConfig)}:{nameof(ElasticsearchConfig.Url)}' " +
                "must be an absolute URL.");
        }

        ValidateRequiredValue(
            elasticsearch.Username,
            $"{nameof(ElasticsearchConfig)}:{nameof(ElasticsearchConfig.Username)}");
        ValidateRequiredValue(
            elasticsearch.Password,
            $"{nameof(ElasticsearchConfig)}:{nameof(ElasticsearchConfig.Password)}");
    }

    private static void ValidateJwtConfiguration(IConfiguration configuration)
    {
        var jwt = configuration.GetSection(SectionNames.JwtConfig).Get<JwtConfig>()
            ?? throw new InvalidOperationException("Jwt configuration is missing or invalid.");

        ValidateRequiredValue(jwt.Issuer, $"{SectionNames.JwtConfig}:{nameof(JwtConfig.Issuer)}");
        ValidateRequiredValue(jwt.Audience, $"{SectionNames.JwtConfig}:{nameof(JwtConfig.Audience)}");
        ValidateRequiredValue(jwt.SigningKey, $"{SectionNames.JwtConfig}:{nameof(JwtConfig.SigningKey)}");

        if (jwt.GuestLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException(
                $"Configuration value '{SectionNames.JwtConfig}:{nameof(JwtConfig.GuestLifetimeMinutes)}' " +
                "must be positive.");
        }
    }

    private static void ValidateRequiredValue(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value '{name}' is required.");
        }
    }
}
