using FluentAssertions;
using Reqnroll;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;

namespace SyncBar.Specs.StepDefinitions.Domain.Entities;

[Binding]
[Scope(Feature = "Log de webhook do Asaas")]
public sealed class AsaasIntegrationWebhookLogSteps
{
    private AsaasIntegrationWebhookLog? _log;
    private Result<AsaasIntegrationWebhookLog>? _createResult;
    private Result? _actionResult;

    [When(@"eu tento registrar o webhook da empresa (.*) com evento vazio e payload ""(.*)""")]
    public void WhenEuTentoRegistrarOWebhookComEventoVazioEPayload(long companyId, string payload)
        => _createResult = AsaasIntegrationWebhookLog.Create(companyId, null, string.Empty, "evt-1", "pay_1", payload);

    [When(@"eu tento registrar o webhook da empresa (.*) com evento ""(.*)"" e payload vazio")]
    public void WhenEuTentoRegistrarOWebhookComEventoEPayloadVazio(long companyId, string eventName)
        => _createResult = AsaasIntegrationWebhookLog.Create(companyId, null, eventName, "evt-1", "pay_1", string.Empty);

    [When(@"eu tento registrar o webhook da empresa (.*) com evento ""(.*)"" e payload ""(.*)""")]
    public void WhenEuTentoRegistrarOWebhookComEventoEPayload(long companyId, string eventName, string payload)
        => _createResult = AsaasIntegrationWebhookLog.Create(companyId, null, eventName, "evt-1", "pay_1", payload);

    [Given(@"um log de webhook da empresa (.*) com evento ""(.*)"" esta registrado")]
    public void GivenUmLogDeWebhookComEventoEstaRegistrado(long companyId, string eventName)
        => _log = AsaasIntegrationWebhookLog.Create(companyId, null, eventName, "evt-1", "pay_1", "{}").Value;

    [Given(@"o log ja foi marcado como processado")]
    public void GivenOLogJaFoiMarcadoComoProcessado()
        => _log!.MarkAsProcessed();

    [When(@"eu marco o log como processado")]
    public void WhenEuMarcoOLogComoProcessado()
        => _actionResult = _log!.MarkAsProcessed();

    [When(@"eu tento marcar o log como falha com mensagem vazia")]
    public void WhenEuTentoMarcarOLogComoFalhaComMensagemVazia()
        => _actionResult = _log!.MarkAsFailed(string.Empty);

    [When(@"eu tento marcar o log como falha com mensagem ""(.*)""")]
    public void WhenEuTentoMarcarOLogComoFalhaComMensagem(string errorMessage)
        => _actionResult = _log!.MarkAsFailed(errorMessage);

    [Then(@"a operacao da entidade deve falhar com o erro ""(.*)""")]
    public void ThenAOperacaoDaEntidadeDeveFalharComOErro(string errorCode)
    {
        var result = _actionResult ?? _createResult;
        result!.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(errorCode);
    }

    [Then(@"a operacao da entidade deve ter sucesso")]
    public void ThenAOperacaoDaEntidadeDeveTerSucesso()
    {
        if (_createResult is not null)
        {
            _createResult.IsSuccess.Should().BeTrue();
            _log = _createResult.Value;
        }
        else
        {
            _actionResult!.IsSuccess.Should().BeTrue();
        }
    }

    [Then(@"o status do log deve ser ""(.*)""")]
    public void ThenOStatusDoLogDeveSer(string status)
        => _log!.Status.ToString().Should().Be(status);
}
