using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TextToSpeech.Infra;

namespace TextToSpeech.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithCleanUp(true)
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
