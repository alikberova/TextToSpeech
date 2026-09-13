using EpubSharp;
using TextToSpeech.Core.Interfaces;

namespace TextToSpeech.Infra.Services.FileProcessing;

public sealed class EpubProcessor : IFileProcessor // another package: VersOne.Epub
{
    public bool CanProcess(string fileType) => fileType.Equals(".epub", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExtractTextAsync(byte[] fileBytes)
    {
        var book = EpubReader.Read(fileBytes);

        return await Task.FromResult(book.ToPlainText());
    }
}
