using Microsoft.Extensions.Hosting;
using TextToSpeech.Infra.Config;

namespace TextToSpeech.Infra;

public static class HostingEnvironment
{
    public const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";

    public static string Current => EnsureAspNetCoreEnvironment();

    public static string EnsureAspNetCoreEnvironment()
    {
        var environment = Environment.GetEnvironmentVariable(AspNetCoreEnvironment);

        if (!string.IsNullOrWhiteSpace(environment))
        {
            return environment;
        }

        environment = Environments.Production;

        Environment.SetEnvironmentVariable(AspNetCoreEnvironment, environment);

        return environment;
    }

    public static bool IsDevelopment()
    {
        return Current == Environments.Development;
    }

    public static bool IsWindows()
    {
        return Environment.OSVersion.ToString().Contains("Windows");
    }

    public static bool IsTestMode()
    {
        return bool.TryParse(Environment.GetEnvironmentVariable(ConfigConstants.IsTestMode)?.Trim(), out var isTestMode)
            && isTestMode;
    }
}
