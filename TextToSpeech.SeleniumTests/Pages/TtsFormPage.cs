using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TextToSpeech.Infra;
using TextToSpeech.SeleniumTests.Extensions;
using static TextToSpeech.SeleniumTests.Constants.UiConstants;

namespace TextToSpeech.SeleniumTests.Pages;

public sealed class TtsFormPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public TtsFormPage(IWebDriver driver, WebDriverWait wait)
    {
        _driver = driver;
        _wait = wait;

        _wait.UntilVisible(By.CssSelector($"[data-testid='{DataTestId.TtsForm}']"));
    }

    private static By DownloadButtonBy => By.CssSelector($"button[data-testid='{DataTestId.DownloadBtn}']");
    private IWebElement ProviderSelect => _driver.FindElement(By.CssSelector($"mat-select[name='{NameAttributes.Provider}']"));
    private IWebElement? LanguageSelect => _driver.FindElements(By.CssSelector($"mat-select[name='{NameAttributes.Language}']")).FirstOrDefault();
    private IWebElement VoiceSelect => _driver.FindElement(By.CssSelector($"mat-select[name='{NameAttributes.Voice}']"));
    private IWebElement FileInput => _driver.FindElement(By.Id(Ids.FileInput));
    private IWebElement SampleTextArea => _driver.FindElement(By.CssSelector($"textarea[data-testid='{DataTestId.SampleTextArea}']"));
    private IWebElement? SamplePlayButton => _driver.FindElement(By.CssSelector($"button[data-testid='{DataTestId.SamplePlayButton}']"));
    private IWebElement SubmitButton => _driver.FindElement(By.CssSelector($"button[data-testid='{DataTestId.SubmitBtn}']"));
    private IWebElement DownloadButton => _wait.UntilVisibleAndEnabled(DownloadButtonBy);
    public bool IsProgressPanelVisible() => _driver.FindElements(By.CssSelector(Selectors.ProgressPanel)).Count != 0;

    public void ClickCancelWhenReady()
    {
        _wait.Until(driver =>
        {
            try
            {
                var cancelButton = driver.FindElements(By.CssSelector(Selectors.ProgressCancelButton))
                    .FirstOrDefault(element => element.Displayed && element.Enabled);

                if (cancelButton is null)
                {
                    return false;
                }

                cancelButton.Click();
                return true;
            }
            catch (ElementClickInterceptedException)
            {
                return false;
            }
        });
    }

    public bool IsIconVisible(string icon)
    {
        return _wait.Until(_ => SamplePlayButton?.Text.Trim() == icon);
    }

    public string? WaitStatusIconText(string status)
    {
        IWebElement? icon = null;

        try
        {
            icon = _wait.UntilElement(By.CssSelector(Selectors.StatusIcon),
                e => e.Text.Trim().Equals(status, StringComparison.OrdinalIgnoreCase));
        }
        catch (WebDriverTimeoutException)
        {
            icon = _driver.FindElement(By.CssSelector(Selectors.StatusIcon));
        }

        return icon?.Text.Trim();
    }

    public void OpenProviderOptions() => ProviderSelect.Click();
    public void OpenLanguageOptions() => LanguageSelect!.Click();
    public void OpenVoiceOptions() => VoiceSelect.Click();

    public void ClickDropdownOption(string text)
    {
        var option = GetDropdownOption(text);
        option!.Click();
    }

    public void SelectProvider(string provider)
    {
        OpenProviderOptions();
        ClickDropdownOption(provider);
    }

    public void SelectLanguage(string language)
    {
        OpenLanguageOptions();
        ClickDropdownOption(language);
    }

    public async Task SelectVoice(string voice)
    {
        OpenVoiceOptions();
        await Task.Delay(100);
        ClickDropdownOption(voice);
    }

    public void TypeSampleText()
    {
        SampleTextArea.Clear();
        SampleTextArea.SendKeys(TestData.TtsSampleRequest);
    }

    public void UploadFile(string filePath)
    {
        FileInput.SendKeys(filePath);
    }

    public void ClickSubmit() => SubmitButton.Click();
    public void ClickDownload() => DownloadButton.Click();
    public void ClickPlayButton()
    {
        _wait.Until(_ =>
        {
            try
            {
                SamplePlayButton!.Click();
                return true;
            }
            catch (ElementClickInterceptedException)
            {
                return false;
            }
        });
    }

    public void RemoveUploadedFile()
    {
        var remove = _driver.FindElements(By.XPath("//div[contains(@class,'file-info')]//button")).FirstOrDefault();
        remove?.Click();
        // Wait until file info gone
        _wait.Until(d => d.FindElements(By.CssSelector(Selectors.FileInfoName)).Count == 0);
    }

    private IWebElement? GetDropdownOption(string buttonText)
    {
        var lower = buttonText.ToLowerInvariant();

        // translate lowercases the element text 
        return _wait.UntilVisibleAndEnabled(By.XPath(
            $"//mat-option/span[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{lower}')]"));
    }
}
