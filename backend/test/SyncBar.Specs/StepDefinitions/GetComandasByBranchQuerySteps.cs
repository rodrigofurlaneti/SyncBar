using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Comandas;
using SyncBar.Application.Features.Comandas.GetByBranch;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Consultar comandas de uma filial")]
public sealed class GetComandasByBranchQuerySteps
{
    private readonly Mock<IComandaRepository> _comandaRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<Comanda> _comandas = [];
    private Result<IReadOnlyCollection<ComandaResponse>>? _result;

    [Given(@"a filial (.*) nao tem nenhuma comanda cadastrada")]
    public void GivenAFilialNaoTemNenhumaComandaCadastrada(long branchId)
        => _comandaRepository
            .Setup(r => r.GetByBranchAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Comanda>)_comandas.AsReadOnly());

    [Given(@"a filial (.*) tem a comanda ""(.*)"" com status (.*)")]
    public void GivenAFilialTemAComandaComStatus(long branchId, string code, long comandaStatusId)
    {
        _comandas.Add(Comanda.Create(branchId, comandaStatusId, code).Value);

        _comandaRepository
            .Setup(r => r.GetByBranchAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Comanda>)_comandas.AsReadOnly());
    }

    [When(@"eu consulto as comandas da filial (.*)")]
    public async Task WhenEuConsultoAsComandasDaFilial(long branchId)
    {
        var handler = new GetComandasByBranchQueryHandler(
            _comandaRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetComandasByBranchQuery(branchId), CancellationToken.None);
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

    [Then(@"a lista de comandas retornada deve estar vazia")]
    public void ThenAListaDeComandasRetornadaDeveEstarVazia()
        => _result!.Value.Should().BeEmpty();

    [Then(@"a lista de comandas retornada deve conter (.*) comandas")]
    public void ThenAListaDeComandasRetornadaDeveConterComandas(int count)
        => _result!.Value.Should().HaveCount(count);

    [Then(@"a primeira comanda da lista deve ter o codigo ""(.*)""")]
    public void ThenAPrimeiraComandaDaListaDeveTerOCodigo(string code)
        => _result!.Value.First().Code.Should().Be(code);
}
