using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Testcontainers.Redis;
using TextToSpeech.Infra.Config;
using static TextToSpeech.Infra.Config.ConfigConstants;

namespace TextToSpeech.IntegrationTests;

public class TestWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
    private readonly RedisContainer _cacheContainer;
    private readonly PostgreSqlFixture _dbContainer;
    private readonly RabbitMqFixture _rabbitMq = new();
    private readonly DirectoryInfo _artifactsDirectory = Directory.CreateTempSubdirectory("tts-tests-");

    public static string CacheConnectionEnv => $"ConnectionStrings__{ConnectionStrings.CacheConnection}";
    public static string DbConnectionEnv => $"ConnectionStrings__{ConnectionStrings.DbConnection}";

    public HttpClient? HttpClient { get; private set; }

    public TestWebApplicationFactory()
    {
        _cacheContainer = new RedisBuilder()
            .WithCleanUp(true)
            .Build();

        _dbContainer = new PostgreSqlFixture();
    }

    public async Task InitializeAsync()
    {
        await _cacheContainer.StartAsync();
        await _dbContainer.InitializeAsync();
        await _rabbitMq.InitializeAsync();

        HttpClient = CreateClient();
    }

    public new async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        await base.DisposeAsync();
        await _cacheContainer.DisposeAsync();
        await _dbContainer.DisposeAsync();
        await _rabbitMq.DisposeAsync();

        if (_artifactsDirectory.Exists)
        {
            _artifactsDirectory.Delete(recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // local, no docker
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == null)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", Environments.Development);
        }

        Environment.SetEnvironmentVariable(IsTestMode, "true");
        Environment.SetEnvironmentVariable(DbConnectionEnv, _dbContainer.ConnectionString);
        Environment.SetEnvironmentVariable(CacheConnectionEnv, _cacheContainer.GetConnectionString());

        builder.ConfigureAppConfiguration((_, configuration) => CreateTestConfiguration());
    }

    private Dictionary<string, string?> CreateTestConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            [AppDataPath] = _artifactsDirectory.FullName
        };

        AddRabbitMqConnectionTestValues(values);

        return values;
    }

    private void AddRabbitMqConnectionTestValues(Dictionary<string, string?> values)
    {
        var rabbitMqConnectionBasePath =
            $"{SectionNames.RabbitMqConfig}:{nameof(RabbitMqConfig.RabbitMqConnection)}";

        values[$"{rabbitMqConnectionBasePath}:{nameof(RabbitMqConnectionConfig.HostName)}"] =
            _rabbitMq.HostName;
        values[$"{rabbitMqConnectionBasePath}:{nameof(RabbitMqConnectionConfig.Port)}"] =
            _rabbitMq.Port.ToString();
        values[$"{rabbitMqConnectionBasePath}:{nameof(RabbitMqConnectionConfig.UserName)}"] =
            _rabbitMq.UserName;
        values[$"{rabbitMqConnectionBasePath}:{nameof(RabbitMqConnectionConfig.Password)}"] =
            _rabbitMq.Password;
        values[$"{rabbitMqConnectionBasePath}:{nameof(RabbitMqConnectionConfig.VirtualHost)}"] =
            _rabbitMq.VirtualHost;
    }
}
