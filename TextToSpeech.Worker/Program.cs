using TextToSpeech.Infra;
using TextToSpeech.Worker;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    EnvironmentName = HostingEnvironment.EnsureAspNetCoreEnvironment()
});

builder.Services.AddJobWorker(builder.Configuration);
builder.Services.AddSpeechGeneration(builder.Configuration);

await builder.Build().RunAsync();
