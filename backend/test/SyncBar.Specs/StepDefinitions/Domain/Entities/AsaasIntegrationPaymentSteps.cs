using FluentAssertions;
using Reqnroll;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;

namespace SyncBar.Specs.StepDefinitions.Domain.Entities;

[Binding]
[Scope(Feature = "Cobranca do Asaas")]
public sealed class AsaasIntegrationPaymentSteps
{
    private AsaasIntegrationPayment? _payment;
    private Result<AsaasIntegrationPayment>? _createResult;

    [When(@"eu tento criar a cobranca da filial (.*), pedido (.*), id ""(.*)"" no valor de (.*)")]
    public void WhenEuTentoCriarACobrancaComId(long branchId, long customerOrderId, string asaasPaymentId, decimal value)
        => _createResult = AsaasIntegrationPayment.Create(branchId, customerOrderId, null, asaasPaymentId, "PIX", value, DateTime.UtcNow);

    [When(@"eu tento criar a cobranca da filial (.*), pedido (.*), id vazio, no valor de (.*)")]
    public void WhenEuTentoCriarACobrancaComIdVazio(long branchId, long customerOrderId, decimal value)
        => _createResult = AsaasIntegrationPayment.Create(branchId, customerOrderId, null, string.Empty, "PIX", value, DateTime.UtcNow);

    [Given(@"uma cobranca da filial (.*), pedido (.*), id ""(.*)"", no valor de (.*) esta criada")]
    public void GivenUmaCobrancaEstaCriada(long branchId, long customerOrderId, string asaasPaymentId, decimal value)
        => _payment = AsaasIntegrationPayment.Create(branchId, customerOrderId, null, asaasPaymentId, "PIX", value, DateTime.UtcNow).Value;

    [When(@"eu marco a cobranca como paga com valor liquido de (.*)")]
    public void WhenEuMarcoACobrancaComoPagaComValorLiquidoDe(decimal netValue)
        => _payment!.MarkAsPaid(netValue);

    [When(@"eu tento atualizar o status da cobranca para vazio")]
    public void WhenEuTentoAtualizarOStatusDaCobrancaParaVazio()
        => _payment!.UpdateStatus(string.Empty);

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
        _payment = _createResult.Value;
    }

    [Then(@"o status da cobranca deve ser ""(.*)""")]
    public void ThenOStatusDaCobrancaDeveSer(string status)
        => _payment!.Status.Should().Be(status);

    [Then(@"o valor liquido da cobranca deve ser (.*)")]
    public void ThenOValorLiquidoDaCobrancaDeveSer(decimal netValue)
        => _payment!.NetValue.Should().Be(netValue);
}
