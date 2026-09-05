using FluentAssertions;
using Reqnroll;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;

namespace SyncBar.Specs.StepDefinitions.Domain.Entities;

[Binding]
[Scope(Feature = "Cartao de credito tokenizado do Asaas")]
public sealed class AsaasIntegrationSavedCardSteps
{
    private AsaasIntegrationSavedCard? _card;
    private Result<AsaasIntegrationSavedCard>? _createResult;

    [When(@"eu tento salvar um cartao do cliente (.*) da empresa (.*) com o token vazio")]
    public void WhenEuTentoSalvarUmCartaoComOTokenVazio(long customerId, long companyId)
        => _createResult = AsaasIntegrationSavedCard.Create(customerId, companyId, string.Empty, "VISA", "1111");

    [When(@"eu tento salvar um cartao do cliente (.*) da empresa (.*) com o token ""(.*)""")]
    public void WhenEuTentoSalvarUmCartaoComOToken(long customerId, long companyId, string token)
        => _createResult = AsaasIntegrationSavedCard.Create(customerId, companyId, token, "VISA", "1111");

    [Given(@"um cartao do cliente (.*) da empresa (.*) com o token ""(.*)"" esta salvo")]
    public void GivenUmCartaoComOTokenEstaSalvo(long customerId, long companyId, string token)
        => _card = AsaasIntegrationSavedCard.Create(customerId, companyId, token, "VISA", "1111").Value;

    [Given(@"um cartao padrao do cliente (.*) da empresa (.*) com o token ""(.*)"" esta salvo")]
    public void GivenUmCartaoPadraoComOTokenEstaSalvo(long customerId, long companyId, string token)
        => _card = AsaasIntegrationSavedCard.Create(customerId, companyId, token, "VISA", "1111", isDefault: true).Value;

    [When(@"eu marco o cartao como padrao")]
    public void WhenEuMarcoOCartaoComoPadrao()
        => _card!.SetAsDefault();

    [When(@"eu removo a marcacao de padrao do cartao")]
    public void WhenEuRemovoAMarcacaoDePadraoDoCartao()
        => _card!.RemoveAsDefault();

    [Then(@"a operacao da entidade deve falhar com o erro ""(.*)""")]
    public void ThenAOperacaoDaEntidadeDeveFalharComOErro(string errorCode)
    {
        _createResult!.IsFailure.Should().BeTrue();
        _createResult.Error.Code.Should().Be(errorCode);
    }

    [Then(@"a operacao da entidade deve ter sucesso")]
    public void ThenAOperacaoDaEntidadeDeveTerSucesso()
    {
        _createResult!.IsSuccess.Should().BeTrue();
        _card = _createResult.Value;
    }

    [Then(@"o cartao deve ser o padrao")]
    public void ThenOCartaoDeveSerOPadrao()
        => _card!.IsDefault.Should().BeTrue();

    [Then(@"o cartao nao deve ser o padrao")]
    public void ThenOCartaoNaoDeveSerOPadrao()
        => _card!.IsDefault.Should().BeFalse();
}
