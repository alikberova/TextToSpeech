using System.Text;
using TextToSpeech.Core.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace TextToSpeech.Infra.Services.FileProcessing;

public sealed class PdfProcessor : IFileProcessor
{
    public bool CanProcess(string fileType) => fileType.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExtractTextAsync(byte[] fileBytes)
    {
        var stringBuilder = new StringBuilder();

        using var pdfReader = PdfDocument.Open(fileBytes);

        foreach (var page in pdfReader.GetPages())
        {
            stringBuilder.AppendLine(ContentOrderTextExtractor.GetText(page, addDoubleNewline: true));
        }

        return await Task.FromResult(stringBuilder.ToString());
    }
}
