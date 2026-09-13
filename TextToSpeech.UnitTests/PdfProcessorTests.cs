using TextToSpeech.Infra.Services.FileProcessing;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace TextToSpeech.UnitTests;

public sealed class PdfProcessorTests
{
    private readonly PdfProcessor _pdfProcessor;

    public PdfProcessorTests()
    {
        _pdfProcessor = new PdfProcessor();
    }

    [Fact]
    public void CanProcess_ShouldReturnTrue_ForPdfFileType()
    {
        // Arrange
        string fileType = ".pdf";

        // Act
        bool result = _pdfProcessor.CanProcess(fileType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanProcess_ShouldReturnFalse_ForNonPdfFileType()
    {
        // Arrange
        string fileType = ".txt";

        // Act
        bool result = _pdfProcessor.CanProcess(fileType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExtractContentAsync_ShouldExtractTextFromPdf()
    {
        // Arrange
        PdfDocumentBuilder builder = new();

        PdfPageBuilder page = builder.AddPage(PageSize.A4);

        PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.CourierBold);

        const string expected = "This is a sample text.";

        page.AddText(expected, 16, new PdfPoint(25, 700), font);

        byte[] pdfBytes = builder.Build();

        // Act
        var actual = (await _pdfProcessor.ExtractTextAsync(pdfBytes)).Trim();

        // Assert
        Assert.Equal(expected, actual);
    }
}
