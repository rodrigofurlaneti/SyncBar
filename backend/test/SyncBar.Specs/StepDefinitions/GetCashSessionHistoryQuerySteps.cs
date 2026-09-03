using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Cash.GetHistory;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Consultar historico de sessoes de caixa")]
public sealed class GetCashSessionHistoryQuerySteps
{
    private readonly Mock<ICashSessionRepository> _cashSessionRepository = new();
    private readonly Mock<ICashRegisterRepository> _cashRegisterRepository = new();
    private readonly Mock<IEmployeeRepository> _employeeRepository = new();
    private readonly Mock<ISaleRepository> _saleRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<IReadOnlyCollection<CashSessionHistoryResponse>>? _result;

    [Given(@"a filial (.*) nao tem sessoes de caixa no periodo")]
    public void GivenAFilialNaoTemSessoesDeCaixaNoPeriodo(long branchId)
    {
        _cashSessionRepository
            .Setup(r => r.GetByBranchAndPeriodAsync(branchId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<CashSession>)Array.Empty<CashSession>());
        _cashRegisterRepository
            .Setup(r => r.GetByBranchAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<CashRegister>)Array.Empty<CashRegister>());
        _employeeRepository
            .Setup(r => r.GetByBranchAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Employee>)Array.Empty<Employee>());
    }

    [Given(@"a filial (.*) tem uma sessao de caixa fechada no periodo com uma venda de (.*)")]
    public void GivenAFilialTemUmaSessaoDeCaixaFechadaNoPeriodoComUmaVendaDe(long branchId, decimal saleAmount)
    {
        var register = CashRegister.Create(branchId, "Caixa 1").Value;
        var session = CashSession.Open(register.Id, 1, 0m).Value;
        session.Close(1, saleAmount, saleAmount);
        var sale = Sale.Create(branchId, 1, session.Id, 1, 1, saleAmount, 0m, 0m).Value;

        _cashSessionRepository
            .Setup(r => r.GetByBranchAndPeriodAsync(branchId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<CashSession>)new List<CashSession> { session });
        _cashRegisterRepository
            .Setup(r => r.GetByBranchAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<CashRegister>)new List<CashRegister> { register });
        _employeeRepository
            .Setup(r => r.GetByBranchAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Employee>)Array.Empty<Employee>());
        _saleRepository
            .Setup(r => r.GetByCashSessionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Sale>)new List<Sale> { sale });
    }

    [When(@"eu consulto o historico de caixa da filial (.*) para o mes (.*) do ano (.*)")]
    public async Task WhenEuConsultoOHistoricoDeCaixaDaFilialParaOMesDoAno(long branchId, int month, int year)
    {
        var handler = new GetCashSessionHistoryQueryHandler(
            _cashSessionRepository.Object, _cashRegisterRepository.Object, _employeeRepository.Object,
            _saleRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetCashSessionHistoryQuery(branchId, year, month), CancellationToken.None);
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

    [Then(@"a lista de sessoes retornada deve estar vazia")]
    public void ThenAListaDeSessoesRetornadaDeveEstarVazia()
        => _result!.Value.Should().BeEmpty();

    [Then(@"a lista de sessoes retornada deve conter (.*) sessao")]
    public void ThenAListaDeSessoesRetornadaDeveConterSessao(int count)
        => _result!.Value.Should().HaveCount(count);

    [Then(@"o total de vendas da sessao retornada deve ser (.*)")]
    public void ThenOTotalDeVendasDaSessaoRetornadaDeveSer(decimal salesTotal)
        => _result!.Value.First().SalesTotal.Should().Be(salesTotal);
}
