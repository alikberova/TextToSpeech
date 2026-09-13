using Testcontainers.RabbitMq;

namespace TextToSpeech.IntegrationTests;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder()
        .WithCleanUp(true)
        .Build();

    public string ConnectionString => _container.GetConnectionString();
    public string HostName => ConnectionUri.Host;
    public int Port => ConnectionUri.Port;
    public string UserName => UserInfo[0];
    public string Password => UserInfo[1];
    public string VirtualHost => string.IsNullOrWhiteSpace(ConnectionUri.AbsolutePath.Trim('/'))
        ? "/"
        : ConnectionUri.AbsolutePath.Trim('/');

    private Uri ConnectionUri => new(ConnectionString);
    private string[] UserInfo => ConnectionUri.UserInfo.Split(':', 2);

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
