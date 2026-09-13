using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Net.Http.Headers;
using System.Text.Json;
using TextToSpeech.Core;
using TextToSpeech.Infra;
using Xunit.Abstractions;

namespace TextToSpeech.SeleniumTests;

public class TestBase : IAsyncLifetime
{
    private readonly string _testId = Guid.NewGuid().ToString("N");

    protected string TestDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $"TtsTest_{_testId}");
    protected string DownloadDirectory =>
        Path.Combine(TestDirectory, "Downloads");
    protected ITestOutputHelper? TestOutputHelper { get; init; }
    protected IWebDriver Driver { get; private set; } = default!;
    protected WebDriverWait Wait { get; private set; } = default!;

    public TestBase(ITestOutputHelper? testOutputHelper = null)
    {
        TestOutputHelper = testOutputHelper;
    }

    public async Task InitializeAsync()
    {
        VerifyTestDirectory();

        await SeedTestData();

        var options = new ChromeOptions();

        if (!HostingEnvironment.IsWindows())
        {
            options.AddArgument("--headless=new");
        }

        options.AddArgument("--ignore-certificate-errors");
        options.AddArgument("--ignore-ssl-errors");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddUserProfilePreference("download.default_directory", DownloadDirectory);

        Driver = new ChromeDriver(options);
        Driver.Manage().Window.Maximize();
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);

        Wait = GetWait(Driver);

        Driver.Navigate().GoToUrl($"https://localhost:{AppConstants.ClientPort}");
    }

    public async Task DisposeAsync()
    {
        Driver?.Quit();
        Driver?.Dispose();
        DeleteTestDirectory();
    }

    private static WebDriverWait GetWait(IWebDriver driver)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5))
        {
            PollingInterval = TimeSpan.FromMilliseconds(250)
        };

        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

        return wait;
    }

    private void VerifyTestDirectory()
    {
        DeleteTestDirectory();

        TestOutputHelper?.WriteLine($"Creating directory {TestDirectory}...");
        Directory.CreateDirectory(TestDirectory);
        TestOutputHelper?.WriteLine("Directory created");
    }

    private void DeleteTestDirectory()
    {
        if (Directory.Exists(TestDirectory))
        {
            TestOutputHelper?.WriteLine($"Deleting directory {TestDirectory}");
            Directory.Delete(TestDirectory, true);
        }
    }

    private static async Task SeedTestData()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var baseUrl = config["ApiBaseUrl"] ??
            throw new InvalidOperationException("ApiBaseUrl is not set");

        using var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };

        var authResponse = await client.PostAsync("/api/auth/guest", new StringContent(string.Empty));

        authResponse.EnsureSuccessStatusCode();

        var json = await authResponse.Content.ReadAsStringAsync();

        var token = JsonDocument.Parse(json)
            .RootElement
            .GetProperty("accessToken")
            .GetString();

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidDataException("Guest token is missing in auth response.");
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync("/test/seed", content: null);

        response.EnsureSuccessStatusCode();
    }
}
