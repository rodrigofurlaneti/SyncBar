using OpenQA.Selenium;
using Xunit;

namespace SyncBar.E2ETests;

public sealed class EnvironmentTheoryAttribute : TheoryAttribute
{
    public EnvironmentTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("E2E_RUN") != "1")
            Skip = "Defina E2E_RUN=1 para executar contra o ambiente publicado.";
    }
}

[Trait("Category", "AnonymousAccess")]
public sealed class AnonymousAccessTests
{
    [EnvironmentTheory]
    [InlineData("/")]
    [InlineData("/mesa/1")]
    [InlineData("/garcom")]
    [InlineData("/delivery")]
    [InlineData("/produtos")]
    [InlineData("/complementos")]
    [InlineData("/estoque")]
    [InlineData("/equipe")]
    [InlineData("/usuarios")]
    [InlineData("/faturamento")]
    [InlineData("/cenarios")]
    [InlineData("/relatorios")]
    [InlineData("/preparo")]
    [InlineData("/fechamentos")]
    [InlineData("/turnos")]
    [InlineData("/promocoes")]
    [InlineData("/impressao")]
    [InlineData("/compras")]
    [InlineData("/reservas")]
    [InlineData("/clientes")]
    [InlineData("/acessos")]
    [InlineData("/configuracoes")]
    [InlineData("/integracoes/ifood")]
    [InlineData("/integracoes/ifood/dashboard")]
    [InlineData("/integracoes/ifood/status")]
    [InlineData("/integracoes/ifood/pedidos")]
    [InlineData("/integracoes/ifood/shipping")]
    [InlineData("/integracoes/ifood/logistica")]
    [InlineData("/integracoes/ifood/catalogo")]
    [InlineData("/integracoes/ifood/avaliacoes")]
    [InlineData("/integracoes/ifood/indicadores")]
    [InlineData("/integracoes/ifood/financeiro/relatorios")]
    [InlineData("/integracoes/asaas")]
    [InlineData("/integracoes/keeta")]
    [InlineData("/pracas")]
    [InlineData("/sem-acesso")]
    public void ProtectedRoute_ShouldRequireAuthentication(string path)
    {
        using var browser = new Browser();
        browser.Open(path);
        browser.Wait.Until(driver => new Uri(driver.Url).AbsolutePath == "/login");
        Assert.True(new LoginPage(browser).Username.Displayed);
        Assert.Empty(browser.Driver.FindElements(By.Id("topbar-nav")));
    }

    [EnvironmentTheory]
    [InlineData(390, 844)]
    [InlineData(768, 1024)]
    [InlineData(1440, 1000)]
    public void Login_ShouldFitViewportAndKeepControlsAccessible(int width, int height)
    {
        using var browser = new Browser();
        browser.Driver.Manage().Window.Size = new System.Drawing.Size(width, height);
        browser.Open("/login");
        var page = new LoginPage(browser);
        Assert.True(page.Username.Displayed);
        Assert.True(page.Password.Displayed);
        ((IJavaScriptExecutor)browser.Driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", page.Submit);
        Assert.True(page.Submit.Displayed);
        Assert.True(Assert.IsType<bool>(((IJavaScriptExecutor)browser.Driver).ExecuteScript(
            "return document.documentElement.scrollWidth <= window.innerWidth + 1;")));
    }

    [EnvironmentTheory]
    [InlineData("legalName")]
    [InlineData("tradeName")]
    [InlineData("cnpj")]
    [InlineData("adminName")]
    [InlineData("adminCpf")]
    [InlineData("branchName")]
    [InlineData("adminUserName")]
    [InlineData("adminEmail")]
    [InlineData("adminPassword")]
    public void CompanyRegistration_ShouldRequireField(string field)
    {
        using var browser = new Browser();
        browser.Open("/cadastro");
        var input = browser.Visible($"[data-testid='{field}']");
        Assert.Equal("true", input.GetAttribute("required"));
        Assert.False(Assert.IsType<bool>(((IJavaScriptExecutor)browser.Driver).ExecuteScript(
            "return arguments[0].checkValidity();", input)));
    }
}

