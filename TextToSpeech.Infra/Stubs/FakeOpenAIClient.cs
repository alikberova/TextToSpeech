using OpenAI;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using TextToSpeech.Infra.Services.FileProcessing;

namespace TextToSpeech.Infra.Stubs;

public static class FakeOpenAIClient
{
    public static OpenAIClient Create()
    {
        var handler = new FakeOpenAiHttpHandler();

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://fake.openai.local/")
        };

        return new OpenAIClient(new ApiKeyCredential("test-key"),
            new OpenAIClientOptions
            {
                Endpoint = httpClient.BaseAddress,
                Transport = new HttpClientPipelineTransport(httpClient)
            });
    }
}

sealed class FakeOpenAiHttpHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Post)
        {
            await Delay.RandomMedium(cancellationToken);

            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(AudioFileService.GenerateSilentMp3(2))
            };

            return resp;
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}

