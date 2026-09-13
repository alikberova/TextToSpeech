using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TextToSpeech.Infra.Config;
using static TextToSpeech.Infra.Config.ConfigConstants;

namespace TextToSpeech.IntegrationTests;

public class TestWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
    private readonly RedisContainer _cacheContainer;
    private readonly PostgreSqlContainer _dbContainer;

    public static string CacheConnectionEnv => $"ConnectionStrings__{ConnectionStrings.CacheConnection}";
    public static string DbConnectionEnv => $"ConnectionStrings__{ConnectionStrings.DbConnection}";

    public HttpClient HttpClient { get; private set; } = null!;

    public TestWebApplicationFactory()
    {
        _cacheContainer = new RedisBuilder()
            .WithCleanUp(true)
            .Build();

        _dbContainer = new PostgreSqlBuilder()
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _cacheContainer.StartAsync();
        await _dbContainer.StartAsync();

        HttpClient = CreateClient();
    }

    public new async Task DisposeAsync()
    {
        await _cacheContainer.DisposeAsync();
        await _dbContainer.DisposeAsync();

        HttpClient.Dispose();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // local, no docker
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == null)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", Environments.Development);
        }

        Environment.SetEnvironmentVariable(ConfigConstants.IsTestMode, "true");
        Environment.SetEnvironmentVariable(DbConnectionEnv, _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable(CacheConnectionEnv, _cacheContainer.GetConnectionString());
    }
}