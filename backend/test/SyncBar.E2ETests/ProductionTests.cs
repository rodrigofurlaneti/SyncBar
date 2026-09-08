using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SyncBar.E2ETests;

public sealed class ProductionFactAttribute : FactAttribute
{
    public ProductionFactAttribute(bool authenticated = false)
    {
        if (Environment.GetEnvironmentVariable("E2E_RUN") != "1")
            Skip = "Defina E2E_RUN=1 para executar contra o ambiente publicado.";
        else if (authenticated && Environment.GetEnvironmentVariable("E2E_AUTH") != "1")
            Skip = "Defina E2E_AUTH=1 e as credenciais de teste para executar o login real.";
    }
}

internal static class Settings
{
    public static Uri BaseUrl
    {
        get
        {
            var uri = new Uri(Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://9.205.156.87:84");
            if (uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidOperationException("E2E_BASE_URL deve ser HTTP(S), sem credenciais na URL.");
            return uri;
        }
    }

    public static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value : throw new InvalidOperationException($"Configure {name} no ambiente.");
}

internal sealed class Browser : IDisposable
{
    public IWebDriver Driver { get; }
    public WebDriverWait Wait { get; }

    public Browser()
    {
        var options = new ChromeOptions();
        if (Environment.GetEnvironmentVariable("E2E_HEADLESS") != "0") options.AddArgument("--headless=new");
        options.AddArgument("--window-size=1440,1000");
        Driver = new ChromeDriver(options);
        Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
        Wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
    }

    public void Open(string path) => Driver.Navigate().GoToUrl(new Uri(Settings.BaseUrl, path));
    public IWebElement Visible(string selector) => Wait.Until(driver =>
    {
        var element = driver.FindElement(By.CssSelector(selector));
        return element.Displayed ? element : null;
    });
    public void Dispose() => Driver.Dispose();
}

internal sealed class LoginPage(Browser browser)
{
    public IWebElement Username => browser.Visible("[data-testid='username']");
    public IWebElement Password => browser.Visible("[data-testid='password']");
    public IWebElement Submit => browser.Visible("[data-testid='submit-login']");

    public void SignIn(string username, string password)
    {
        Username.SendKeys(username);
        Password.SendKeys(password);
        Submit.Click();
        browser.Wait.Until(driver =>
        {
            if (driver.FindElements(By.CssSelector(".swal2-error")).Any(element => element.Displayed))
                throw new InvalidOperationException("A aplicação rejeitou o login. Verifique a API e a conta E2E.");
            return new Uri(driver.Url).AbsolutePath == "/" &&
                driver.FindElements(By.Id("topbar-nav")).Any(element => element.Displayed);
        });
    }
}

[Trait("Category", "ProductionSmoke")]
public sealed class ProductionTests
{
    [ProductionFact]
    public void Login_ShouldRenderUsableForm()
    {
        using var browser = new Browser();
        browser.Open("/login");
        var page = new LoginPage(browser);
        Assert.True(page.Username.Enabled);
        Assert.Equal("password", page.Password.GetAttribute("type"));
        Assert.True(page.Submit.Enabled);
        Assert.True(browser.Visible("[data-testid='system-logo']").Displayed);
    }

    [ProductionFact]
    public void Login_ShouldRequireBothCredentials()
    {
        using var browser = new Browser();
        browser.Open("/login");
        var page = new LoginPage(browser);
        // Verifica a validação HTML sem enviar credenciais inválidas à API.
        Assert.Equal("true", page.Username.GetAttribute("required"));
        Assert.Equal("true", page.Password.GetAttribute("required"));
        Assert.False(Assert.IsType<bool>(((IJavaScriptExecutor)browser.Driver).ExecuteScript(
            "return arguments[0].checkValidity();", page.Username)));
    }

    [ProductionFact]
    public void ProtectedDelivery_ShouldRedirectAnonymousVisitorToLogin()
    {
        using var browser = new Browser();
        browser.Open("/delivery");
        browser.Wait.Until(driver => new Uri(driver.Url).AbsolutePath == "/login");
        Assert.True(new LoginPage(browser).Username.Displayed);
    }

    [ProductionFact(authenticated: true)]
    public void RealLogin_ShouldOpenAuthenticatedPanelAndSurviveReload()
    {
        if (Settings.BaseUrl.Scheme != "https" && Environment.GetEnvironmentVariable("E2E_ALLOW_HTTP_LOGIN") != "1")
            throw new InvalidOperationException("Use HTTPS ou defina E2E_ALLOW_HTTP_LOGIN=1 para autorizar credenciais sem criptografia.");
        var username = Settings.Required("E2E_USERNAME");
        var password = Settings.Required("E2E_PASSWORD");
        using var browser = new Browser();
        browser.Open("/login");
        new LoginPage(browser).SignIn(username, password);
        browser.Driver.Navigate().Refresh();
        Assert.True(browser.Visible("#topbar-nav").Displayed);
        Assert.Equal("/", new Uri(browser.Driver.Url).AbsolutePath);
    }
}
