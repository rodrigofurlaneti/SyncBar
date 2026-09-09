using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace SyncBar.E2ETests;

[Trait("Category", "CustomerSignup")]
public sealed class CustomerSignupTests(ITestOutputHelper output)
{
    [CustomerSignupTheory]
    [InlineData("API")]
    [InlineData("Desktop")]
    [InlineData("Android")]
    public async Task NewCustomer_ShouldRegisterLoginAndPersistAddress(string profile)
    {
        var settings = new CustomerTestSettings();
        using var probe = new CustomerApiProbe(settings, output);
        var companyId = await probe.CompanyId(); // Check target before reserving a name.
        var data = CustomerTestData.Create(settings.SequenceFile);
        output.WriteLine($"INÍCIO {DateTime.UtcNow:O}: {profile}; {data.Name}; destino={settings.BaseUrl.GetLeftPart(UriPartial.Authority)}; filial={settings.BranchId}. O registro será mantido.");
        if (profile == "API")
        {
            var id = await probe.Register(data);
            await probe.VerifyAccountAndAddress(data, companyId, id, createAddress: true);
            return;
        }

        using var browser = new Browser(android: profile == "Android");
        browser.Wait.Timeout = TimeSpan.FromSeconds(45);
        output.WriteLine("ETAPA UI: abrir cardápio e selecionar produto.");
        browser.Open($"/cardapio/{settings.BranchId}");
        var productSelector = Environment.GetEnvironmentVariable("E2E_PRODUCT_ID") is { Length: > 0 } productId
            ? $"[data-testid='btn-add-item-{long.Parse(productId)}']" : "[data-testid^='btn-add-item-']";
        Click(browser, browser.Visible(productSelector));
        browser.Wait.Until(driver => driver.FindElements(By.CssSelector("dialog.product-wizard")).Any(e => e.Displayed) ||
            driver.FindElements(By.CssSelector("[data-testid='btn-open-cart']")).Any(e => e.GetAttribute("aria-label")?.Contains("com 1 itens") == true));
        CompleteWizard(browser);
        output.WriteLine("ETAPA UI: abrir cadastro e verificar campos obrigatórios.");
        Click(browser, browser.Visible("[data-testid='btn-open-cart']"));
        Click(browser, browser.Visible("[data-testid='btn-submit-order']"));
        var modal = browser.Visible("[data-testid='storefront-auth-modal']");
        Click(browser, modal.FindElements(By.CssSelector("[role='tab']")).Single(e => e.Text.Contains("Novo Cliente")));

        Click(browser, browser.Visible("[data-testid='storefront-auth-modal'] button[type='submit']"));
        Assert.True(browser.Visible("[data-testid='storefront-auth-modal'] [role='alert']").Displayed);

        Fill(browser, "reg-name", data.Name);
        Fill(browser, "reg-cpf", data.Cpf);
        Fill(browser, "reg-email", data.Email);
        Fill(browser, "reg-pass", data.Password);
        Fill(browser, "reg-confirm-pass", data.Password);
        Fill(browser, "reg-cep", CustomerTestData.ZipCode);
        // ViaCEP is a real dependency; after its bounded lookup, manual entry is allowed
        // by the product. Only synthesize the address if the lookup has not supplied it.
        browser.Wait.Until(driver => !driver.FindElements(By.CssSelector(".auth-manual-address")).Any(e => e.Displayed));
        if (string.IsNullOrWhiteSpace(browser.Visible("[id$='-reg-street']").GetAttribute("value")))
        {
            output.WriteLine("ViaCEP não preencheu a rua; validando o caminho de preenchimento manual.");
            Fill(browser, "reg-street", "Endereço sintético E2E");
        }
        if (string.IsNullOrWhiteSpace(browser.Visible("[id$='-reg-neighborhood']").GetAttribute("value")))
            Fill(browser, "reg-neighborhood", "Bairro de teste");
        Fill(browser, "reg-number", "1");
        var submit = browser.Visible("[data-testid='storefront-auth-modal'] button[type='submit']");
        browser.Wait.Until(_ => submit.Enabled);
        output.WriteLine($"ETAPA UI {DateTime.UtcNow:O}: enviar cadastro de {data.Name} e aguardar login/endereço.");
        Click(browser, submit);
        browser.Wait.Until(driver =>
        {
            if (driver.FindElements(By.CssSelector("[data-testid='storefront-auth-modal'] [role='alert']")).Any(e => e.Displayed))
                throw new InvalidOperationException($"Cadastro rejeitado na interface ({profile}, {data.Name}). Consulte os logs de customers/customer-login/addresses no horário registrado.");
            return !driver.FindElements(By.CssSelector("[data-testid='storefront-auth-modal']")).Any(e => e.Displayed);
        });
        Assert.Contains(data.Name, browser.Visible("[data-testid='public-cart-drawer']").Text);
        output.WriteLine("ETAPA UI: cliente identificado no carrinho; verificando persistência pela API.");
        // A fresh HTTP client session proves the browser did not merely close its modal.
        await probe.VerifyAccountAndAddress(data, companyId, expectedCustomerId: null, createAddress: false);
    }

    private static void CompleteWizard(Browser browser)
    {
        for (var step = 0; step < 30 && browser.Driver.FindElements(By.CssSelector("dialog.product-wizard")).Any(e => e.Displayed); step++)
        {
            var action = browser.Visible("[data-testid='btn-confirm-complements']");
            while (!action.Enabled)
            {
                var option = browser.Driver.FindElements(By.CssSelector("dialog.product-wizard input[type='checkbox']"))
                    .FirstOrDefault(e => e.Enabled && !e.Selected);
                Assert.NotNull(option);
                Click(browser, option.FindElement(By.XPath("..")));
            }
            var heading = browser.Visible(".pw-step-content h2");
            Click(browser, action);
            browser.Wait.Until(driver =>
            {
                try { return !heading.Displayed; }
                catch (StaleElementReferenceException) { return true; }
            });
        }
        Assert.Empty(browser.Driver.FindElements(By.CssSelector("dialog.product-wizard")));
    }

    private static void Fill(Browser browser, string suffix, string value)
    {
        var input = browser.Visible($"[data-testid='storefront-auth-modal'] [id$='-{suffix}']");
        ((IJavaScriptExecutor)browser.Driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", input);
        input.Clear();
        input.SendKeys(value);
    }

    private static void Click(Browser browser, IWebElement element)
    {
        ((IJavaScriptExecutor)browser.Driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", element);
        element.Click(); // Real WebDriver click; do not bypass overlays with JavaScript clicks.
    }
}
