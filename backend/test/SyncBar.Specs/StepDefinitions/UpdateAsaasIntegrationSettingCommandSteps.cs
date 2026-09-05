using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Integrations.Asaas.Setting.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Atualizar configuracao Asaas")]
public sealed class UpdateAsaasIntegrationSettingCommandSteps
{
    private readonly Mock<IAsaasIntegrationSettingRepository> _settingRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result? _result;

    [Given(@"a configuracao Asaas com id (.*) nao esta cadastrada")]
    public void GivenAConfiguracaoAsaasComIdNaoEstaCadastrada(long id)
        => _settingRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AsaasIntegrationSetting?)null);

    [Given(@"uma configuracao Asaas com id (.*) da empresa (.*) esta cadastrada")]
    public void GivenUmaConfiguracaoAsaasComIdDaEmpresaEstaCadastrada(long id, long companyId)
        => _settingRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AsaasIntegrationSetting.Create(companyId, null, "chave-atual").Value);

    [When(@"eu tento atualizar a configuracao (.*) da empresa (.*) com a chave de API vazia")]
    public async Task WhenEuTentoAtualizarAConfiguracaoComAChaveDeApiVazia(long id, long companyId)
        => await UpdateAsync(id, companyId, "   ");

    [When(@"eu tento atualizar a configuracao (.*) da empresa (.*) com a chave de API ""(.*)""")]
    public async Task WhenEuTentoAtualizarAConfiguracaoComAChaveDeApi(long id, long companyId, string apiKey)
        => await UpdateAsync(id, companyId, apiKey);

    private async Task UpdateAsync(long id, long companyId, string apiKey)
    {
        var handler = new UpdateAsaasIntegrationSettingCommandHandler(_settingRepository.Object, _logRepository.Object, _unitOfWork.Object);
        _result = await handler.Handle(
            new UpdateAsaasIntegrationSettingCommand(id, companyId, apiKey),
            CancellationToken.None);
    }

    [Then(@"a operacao deve falhar com o erro ""(.*)""")]
    public void ThenAOperacaoDeveFalharComOErro(string errorCode)
    {
        _result!.IsFailure.Should().BeTrue();
        _result.Error.Code.Should().Be(errorCode);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();
}
