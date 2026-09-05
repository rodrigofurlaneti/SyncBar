using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Integrations.Asaas.Setting.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Criar configuracao Asaas")]
public sealed class CreateAsaasIntegrationSettingCommandSteps
{
    private readonly Mock<IAsaasIntegrationSettingRepository> _settingRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<CreateAsaasIntegrationSettingResponse>? _result;

    [Given(@"ja existe uma configuracao Asaas para a empresa (.*) sem filial")]
    public void GivenJaExisteUmaConfiguracaoAsaasParaAEmpresaSemFilial(long companyId)
        => _settingRepository
            .Setup(r => r.GetByScopeAsync(companyId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AsaasIntegrationSetting.Create(companyId, null, "chave-existente").Value);

    [When(@"eu tento criar a configuracao Asaas para a empresa (.*) sem filial com a chave de API vazia")]
    public async Task WhenEuTentoCriarAConfiguracaoAsaasSemFilialComAChaveDeApiVazia(long companyId)
        => await CreateAsync(companyId, string.Empty);

    [When(@"eu tento criar a configuracao Asaas para a empresa (.*) sem filial com a chave de API ""(.*)""")]
    public async Task WhenEuTentoCriarAConfiguracaoAsaasSemFilialComAChaveDeApi(long companyId, string apiKey)
        => await CreateAsync(companyId, apiKey);

    private async Task CreateAsync(long companyId, string apiKey)
    {
        var handler = new CreateAsaasIntegrationSettingCommandHandler(_settingRepository.Object, _logRepository.Object, _unitOfWork.Object);
        _result = await handler.Handle(
            new CreateAsaasIntegrationSettingCommand(companyId, null, apiKey, null, "Sandbox", true),
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

    [Then(@"a configuracao criada deve ser adicionada ao repositorio da empresa (.*)")]
    public void ThenAConfiguracaoCriadaDeveSerAdicionadaAoRepositorioDaEmpresa(long companyId)
        => _settingRepository.Verify(
            r => r.AddAsync(It.Is<AsaasIntegrationSetting>(s => s.CompanyId == companyId), It.IsAny<CancellationToken>()),
            Times.Once);
}
