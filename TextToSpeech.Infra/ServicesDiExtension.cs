using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TextToSpeech.Infra.Config;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Infra.Services;
using static TextToSpeech.Infra.Config.ConfigConstants;

namespace TextToSpeech.Infra;

public static class ServicesDiExtension
{
    public static void AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var conn = configuration.GetConnectionString(ConnectionStrings.CacheConnection)!;
            ConfigurationOptions options = ConfigurationOptions.Parse(conn);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddScoped<IRedisCacheProvider, RedisCacheProvider>();
    }
}
