using System.Reflection;
using TextToSpeech.Infra.Config;

namespace TextToSpeech.Api.Extensions;

public static class ConfigurationExtension
{
    public static IConfigurationBuilder SetConfig(this IConfigurationManager configurationBuilder)
    {
        if (string.IsNullOrWhiteSpace(configurationBuilder[ConfigConstants.AppDataPath]))
        {
            configurationBuilder[ConfigConstants.AppDataPath] =
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        var config = configurationBuilder
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        if (Infra.HostingEnvironment.IsTestMode())
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ConfigConstants.ElevenLabsApiKey] = string.Empty,
                [ConfigConstants.OpenAiApiKey] = string.Empty,
                [$"{nameof(NarakeetConfig)}:{nameof(NarakeetConfig.ApiKey)}"] = string.Empty,
            });
        }

        return config;
    }
}
