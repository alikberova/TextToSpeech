using TextToSpeech.Core.Interfaces;

namespace TextToSpeech.Infra.Services.FileProcessing;

public sealed class TextFileProcessor : IFileProcessor
{
    public bool CanProcess(string fileType) => fileType.Equals(".txt", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(byte[] fileBytes)
    {
        var text = System.Text.Encoding.UTF8.GetString(fileBytes);

        return Task.FromResult(text);
    }
}
