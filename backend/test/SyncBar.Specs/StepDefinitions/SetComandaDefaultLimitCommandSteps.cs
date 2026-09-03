using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Comandas.Settings;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class SetComandaDefaultLimitCommandSteps
{
    private readonly Mock<IComandaSettingRepository> _settingRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result? _result;

    [Given(@"a filial (.*) ainda nao tem configuracao de limite de comanda cadastrada")]
    public void GivenAFilialAindaNaoTemConfiguracaoDeLimiteDeComandaCadastrada(long branchId)
        => _settingRepository
            .Setup(r => r.GetByBranchForUpdateAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComandaSetting?)null);

    [Given(@"a filial (.*) ja tem uma configuracao de limite de comanda cadastrada")]
    public void GivenAFilialJaTemUmaConfiguracaoDeLimiteDeComandaCadastrada(long branchId)
    {
        var setting = ComandaSetting.Create(branchId, 50m).Value;
        _settingRepository
            .Setup(r => r.GetByBranchForUpdateAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(setting);
    }

    [When(@"eu defino o limite padrao de comanda da filial (.*) como (.*)")]
    public async Task WhenEuDefinoOLimitePadraoDeComandaDaFilialComo(long branchId, decimal limitAmount)
    {
        var handler = new SetComandaDefaultLimitCommandHandler(
            _settingRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new SetComandaDefaultLimitCommand(branchId, limitAmount), CancellationToken.None);
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
