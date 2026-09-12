using System.Diagnostics;
using System.Text;
using TextToSpeech.Infra.Constants;
using TextToSpeech.SeleniumTests.Pages;
using Xunit.Abstractions;
using static TextToSpeech.Infra.TestData;
using static TextToSpeech.SeleniumTests.Constants.UiConstants;

namespace TextToSpeech.SeleniumTests.Tests;

/// <summary>
/// End-to-end tests for Home page behaviors against a running app and API.
/// Uses pre-seeded DB data (Infra.TestData) and SignalR to validate generation,
/// download, and cancellation scenarios.
/// </summary>
public sealed class TtsFormTests(ITestOutputHelper output) : TestBase(output)
{
    private TtsFormPage CreatePage() => new(Driver, Wait);

    [Fact]
    public async Task ShouldGenerateSpeech_AndDownload_PreSeededDbAudio()
    {
        const string fileName = "integration-seeded";
        const string sourceExt = "txt";
        const string expectedAudioExt = "mp3";

        var sourcePath = Path.Combine(TestDirectory, $"{fileName}.{sourceExt}");
        File.WriteAllText(sourcePath, TtsFullRequest);

        var page = CreatePage();

        page.SelectProvider(Shared.OpenAI.Name);
        await page.SelectVoice(OpenAiVoices.Alloy.Name);

        page.TypeSampleText();

        page.ClickPlayButton();
        Assert.True(page.IsIconVisible("pause_circle"));

        page.ClickPlayButton();
        Assert.True(page.IsIconVisible("play_circle"));

        page.SelectProvider(Shared.OpenAI.Name);
        await page.SelectVoice(OpenAiVoices.Fable.Name);
        page.UploadFile(sourcePath);
        page.ClickSubmit();

        Assert.True(page.IsProgressPanelVisible());

        page.ClickDownload();

        var sw = Stopwatch.StartNew();

        var expectedPath = Path.Combine(DownloadDirectory, $"{fileName}.{expectedAudioExt}");

        while (sw.ElapsedMilliseconds < 3000 && !File.Exists(expectedPath))
        {
            await Task.Delay(100);
        }

        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public async Task ShouldCancelSpeechProcessing()
    {
        var path1 = Path.Combine(TestDirectory, "test.txt");
        await File.WriteAllTextAsync(path1, "abc");
        var path2 = Path.Combine(TestDirectory, "processing.txt");
        await WriteBigFileText(path2, TtsFullRequest, 500);

        var page = CreatePage();

        page.SelectProvider(Shared.Narakeet.Name);
        page.SelectLanguage(Lang.GermanStandard);
        await page.SelectVoice(NarakeetVoices.Hans.Name);
        
        page.UploadFile(path1);
        page.RemoveUploadedFile();

        page.UploadFile(path2);
        page.ClickSubmit();

        Assert.True(page.IsProgressPanelVisible());

        page.ClickCancelWhenReady();

        Assert.Equal(StatusIcons.Canceled, page.WaitStatusIconText(StatusIcons.Canceled));
    }

    private static async Task WriteBigFileText(string filePath, string content, int repetitions)
    {
        var stringBuilder = new StringBuilder();

        for (int i = 0; i < repetitions; i++)
        {
            stringBuilder.Append(content);
        }

        stringBuilder.Append(Guid.NewGuid());

        await File.WriteAllTextAsync(filePath, stringBuilder.ToString());
    }
}
