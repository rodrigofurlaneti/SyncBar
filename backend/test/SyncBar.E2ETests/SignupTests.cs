using System.Security.Cryptography;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace SyncBar.E2ETests;

public sealed class SignupTests(ITestOutputHelper output)
{
    // Synthetic identifiers with mathematical check digits, not verified real identities.
    private static string Document(bool cnpj)
    {
        var digits = Enumerable.Range(0, cnpj ? 8 : 9).Select(_ => RandomNumberGenerator.GetInt32(10)).ToList();
        if (cnpj) digits.AddRange([0, 0, 0, 1]);
        for (var pass = 0; pass < 2; pass++)
        {
            var weights = cnpj
                ? (pass == 0 ? new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 } : [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2])
                : Enumerable.Range(2, digits.Count).Reverse().ToArray();
            var remainder = digits.Select((digit, i) => digit * weights[i]).Sum() % 11;
            digits.Add(remainder < 2 ? 0 : 11 - remainder);
        }
        return digits.Distinct().Count() == 1 ? Document(cnpj) : string.Concat(digits);
    }

    [ProductionFact(signup: true)]
    public void NewCompany_ShouldRegisterAndAllowAdministratorLogin()
    {
        using var browser = new Browser();
        var suffix = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..25];
        var username = $"e2e.{suffix}";
        var password = $"E2e!{Guid.NewGuid():N}";
        var fields = new Dictionary<string, string>
        {
            ["legalName"] = $"João Furlaneti Teste {suffix} LTDA",
            ["tradeName"] = $"Bar Teste {suffix}",
            ["cnpj"] = Document(true),
            ["adminName"] = $"João Furlaneti Teste {suffix}",
            ["adminCpf"] = Document(false),
            ["branchName"] = "Matriz Teste E2E",
            ["adminUserName"] = username,
            ["adminEmail"] = $"{username}@example.com",
            ["adminPassword"] = password,
        };
        browser.Open("/cadastro");
        foreach (var (field, value) in fields)
            browser.Visible($"[data-testid='{field}']").SendKeys(value);
        browser.Visible("[data-testid='submit-signup']").Click();
        browser.Wait.Until(driver =>
        {
            if (driver.FindElements(By.CssSelector(".swal2-error")).Any(element => element.Displayed))
                throw new InvalidOperationException("Cadastro rejeitado: " + browser.Visible(".swal2-html-container").Text);
            return new Uri(driver.Url).AbsolutePath == "/login";
        });
        output.WriteLine($"Empresa de teste cadastrada: {fields["tradeName"]}; usuário: {username}. Registro mantido no servidor.");
        new LoginPage(browser).SignIn(username, password);
        browser.Driver.Navigate().Refresh();
        Assert.True(browser.Visible("#topbar-nav").Displayed);
    }
}
