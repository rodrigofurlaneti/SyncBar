using FluentAssertions;
using Reqnroll;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;

namespace SyncBar.Specs.StepDefinitions.Domain.Entities;

[Binding]
[Scope(Feature = "Configuracao de integracao Asaas")]
public sealed class AsaasIntegrationSettingSteps
{
    private AsaasIntegrationSetting? _setting;
    private Result? _voidResult;
    private Result<AsaasIntegrationSetting>? _createResult;

    [When(@"eu tento criar a configuracao Asaas para a empresa (.*) sem filial com a chave de API vazia")]
    public void WhenEuTentoCriarAConfiguracaoAsaasSemFilialComAChaveDeApiVazia(long companyId)
        => _createResult = AsaasIntegrationSetting.Create(companyId, null, string.Empty);

    [When(@"eu tento criar a configuracao Asaas para a empresa (.*) sem filial com a chave de API ""(.*)""")]
    public void WhenEuTentoCriarAConfiguracaoAsaasSemFilialComAChaveDeApi(long companyId, string apiKey)
        => _createResult = AsaasIntegrationSetting.Create(companyId, null, apiKey);

    [Given(@"uma configuracao Asaas da empresa (.*) com a chave de API ""(.*)"" esta criada")]
    public void GivenUmaConfiguracaoAsaasDaEmpresaComAChaveDeApiEstaCriada(long companyId, string apiKey)
        => _setting = AsaasIntegrationSetting.Create(companyId, null, apiKey).Value;

    [When(@"eu tento atualizar a chave de API da configuracao para vazia")]
    public void WhenEuTentoAtualizarAChaveDeApiDaConfiguracaoParaVazia()
        => _voidResult = _setting!.UpdateDetails(apiKeyEncrypted: string.Empty);

    [When(@"eu desativo a configuracao")]
    public void WhenEuDesativoAConfiguracao()
        => _setting!.Deactivate();

    [Then(@"a operacao da entidade deve falhar com o erro ""(.*)""")]
    public void ThenAOperacaoDaEntidadeDeveFalharComOErro(string errorCode)
    {
        var result = (Result?)_createResult ?? _voidResult;
        result!.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(errorCode);
    }

    [Then(@"a operacao da entidade deve ter sucesso")]
    public void ThenAOperacaoDaEntidadeDeveTerSucesso()
    {
        _createResult!.IsSuccess.Should().BeTrue();
        _setting = _createResult.Value;
    }

    [Then(@"o ambiente da configuracao deve ser ""(.*)""")]
    public void ThenOAmbienteDaConfiguracaoDeveSer(string environment)
        => _setting!.Environment.Should().Be(environment);

    [Then(@"a chave de API da configuracao deve continuar ""(.*)""")]
    public void ThenAChaveDeApiDaConfiguracaoDeveContinuar(string apiKey)
        => _setting!.ApiKeyEncrypted.Should().Be(apiKey);

    [Then(@"a configuracao deve estar inativa")]
    public void ThenAConfiguracaoDeveEstarInativa()
        => _setting!.IsActive.Should().BeFalse();
}
