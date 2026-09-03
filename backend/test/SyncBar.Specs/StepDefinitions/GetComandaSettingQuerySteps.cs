using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Comandas.Settings;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Consultar configuracao de limite de comanda da filial")]
public sealed class GetComandaSettingQuerySteps
{
    private readonly Mock<IComandaSettingRepository> _settingRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<ComandaSettingResponse>? _result;

    [Given(@"a filial (.*) nao tem configuracao de limite de comanda")]
    public void GivenAFilialNaoTemConfiguracaoDeLimiteDeComanda(long branchId)
        => _settingRepository
            .Setup(r => r.GetByBranchAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComandaSetting?)null);

    [Given(@"a filial (.*) tem o limite padrao de comanda de (.*)")]
    public void GivenAFilialTemOLimitePadraoDeComandaDe(long branchId, decimal limitAmount)
    {
        var setting = ComandaSetting.Create(branchId, limitAmount).Value;
        _settingRepository
            .Setup(r => r.GetByBranchAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(setting);
    }

    [When(@"eu consulto a configuracao de comanda da filial (.*)")]
    public async Task WhenEuConsultoAConfiguracaoDeComandaDaFilial(long branchId)
    {
        var handler = new GetComandaSettingQueryHandler(
            _settingRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetComandaSettingQuery(branchId), CancellationToken.None);
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

    [Then(@"o limite padrao retornado deve ser (.*)")]
    public void ThenOLimitePadraoRetornadoDeveSer(decimal limitAmount)
        => _result!.Value.DefaultLimitAmount.Should().Be(limitAmount);
}
