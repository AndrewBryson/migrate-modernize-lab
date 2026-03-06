namespace ContosoUniversity.Tests;

/// <summary>
/// Base class for all Playwright tests. Configures the base URL from
/// the CONTOSO_BASE_URL environment variable (defaults to the Azure deployment).
/// </summary>
public class TestBase : PageTest
{
    protected string BaseUrl { get; private set; } = null!;

    [SetUp]
    public void SetUpBase()
    {
        BaseUrl = Environment.GetEnvironmentVariable("CONTOSO_BASE_URL")
            ?? "https://appbdbkad4anski6.azurewebsites.net";
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            BaseURL = Environment.GetEnvironmentVariable("CONTOSO_BASE_URL")
                ?? "https://appbdbkad4anski6.azurewebsites.net",
            IgnoreHTTPSErrors = true
        };
    }
}
