using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Integrations.Asaas.Setting.Delete;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Remover configuracao Asaas")]
public sealed class DeleteAsaasIntegrationSettingCommandSteps
{
    private readonly Mock<IAsaasIntegrationSettingRepository> _settingRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AsaasIntegrationSetting? _setting;
    private Result? _result;

    [Given(@"a configuracao Asaas com id (.*) nao esta cadastrada")]
    public void GivenAConfiguracaoAsaasComIdNaoEstaCadastrada(long id)
        => _settingRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AsaasIntegrationSetting?)null);

    [Given(@"uma configuracao Asaas com id (.*) da empresa (.*) esta cadastrada")]
    public void GivenUmaConfiguracaoAsaasComIdDaEmpresaEstaCadastrada(long id, long companyId)
    {
        _setting = AsaasIntegrationSetting.Create(companyId, null, "chave-atual").Value;
        _settingRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_setting);
    }

    [When(@"eu tento remover a configuracao (.*) da empresa (.*)")]
    public async Task WhenEuTentoRemoverAConfiguracaoDaEmpresa(long id, long companyId)
    {
        var handler = new DeleteAsaasIntegrationSettingCommandHandler(_settingRepository.Object, _logRepository.Object, _unitOfWork.Object);
        _result = await handler.Handle(new DeleteAsaasIntegrationSettingCommand(id, companyId), CancellationToken.None);
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

    [Then(@"a configuracao deve ser removida do repositorio")]
    public void ThenAConfiguracaoDeveSerRemovidaDoRepositorio()
        => _settingRepository.Verify(r => r.Delete(_setting!), Times.Once);
}
