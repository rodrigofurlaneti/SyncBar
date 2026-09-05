using FluentAssertions;
using Reqnroll;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;

namespace SyncBar.Specs.StepDefinitions.Domain.Entities;

[Binding]
[Scope(Feature = "Vinculo de cliente com o Asaas")]
public sealed class AsaasIntegrationCustomerSteps
{
    private AsaasIntegrationCustomer? _customer;
    private Result<AsaasIntegrationCustomer>? _createResult;

    [When(@"eu tento vincular o cliente (.*) da empresa (.*) ao Asaas com o id vazio")]
    public void WhenEuTentoVincularOClienteAoAsaasComOIdVazio(long customerId, long companyId)
        => _createResult = AsaasIntegrationCustomer.Create(customerId, companyId, string.Empty);

    [When(@"eu tento vincular o cliente (.*) da empresa (.*) ao Asaas com o id ""(.*)""")]
    public void WhenEuTentoVincularOClienteAoAsaasComOId(long customerId, long companyId, string asaasCustomerId)
        => _createResult = AsaasIntegrationCustomer.Create(customerId, companyId, asaasCustomerId);

    [Given(@"um vinculo do cliente (.*) da empresa (.*) com o AsaasCustomerId ""(.*)"" esta criado")]
    public void GivenUmVinculoDoClienteDaEmpresaComOAsaasCustomerIdEstaCriado(long customerId, long companyId, string asaasCustomerId)
        => _customer = AsaasIntegrationCustomer.Create(customerId, companyId, asaasCustomerId).Value;

    [When(@"eu tento atualizar o AsaasCustomerId do vinculo para vazio")]
    public void WhenEuTentoAtualizarOAsaasCustomerIdDoVinculoParaVazio()
        => _customer!.UpdateAsaasCustomerId(string.Empty);

    [When(@"eu tento atualizar o AsaasCustomerId do vinculo para ""(.*)""")]
    public void WhenEuTentoAtualizarOAsaasCustomerIdDoVinculoPara(string newAsaasCustomerId)
        => _customer!.UpdateAsaasCustomerId(newAsaasCustomerId);

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
        _customer = _createResult.Value;
    }

    [Then(@"o AsaasCustomerId do vinculo deve ser ""(.*)""")]
    [Then(@"o AsaasCustomerId do vinculo deve continuar ""(.*)""")]
    public void ThenOAsaasCustomerIdDoVinculoDeveSer(string asaasCustomerId)
        => _customer!.AsaasCustomerId.Should().Be(asaasCustomerId);
}
