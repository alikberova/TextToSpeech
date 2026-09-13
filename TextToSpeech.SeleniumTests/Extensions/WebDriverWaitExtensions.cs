using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace TextToSpeech.SeleniumTests.Extensions;

public static class WebDriverWaitExtensions
{
    public static IWebElement UntilVisible(this WebDriverWait driver, By by) =>
        driver.UntilElement(by, el => el.Displayed);

    public static IWebElement UntilVisibleAndEnabled(this WebDriverWait driver, By by) =>
        driver.UntilElement(by, el => el.Displayed && el.Enabled);

    public static IWebElement UntilElement(this WebDriverWait wait, By by, Func<IWebElement, bool> condition)
    {
        var element = wait.Until(drv =>
        {
            var el = drv.FindElement(by);
            return condition(el) ? el : null;
        });

        return element ?? throw new WebDriverTimeoutException(
            $"Timed out after seconds waiting for element located by {by}.");
    }
}
