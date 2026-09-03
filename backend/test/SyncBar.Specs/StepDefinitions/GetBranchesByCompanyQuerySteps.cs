using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Branches;
using SyncBar.Application.Features.Branches.GetByCompany;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class GetBranchesByCompanyQuerySteps
{
    private readonly Mock<IBranchRepository> _branchRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<Branch> _branches = new();
    private Result<IReadOnlyCollection<BranchResponse>>? _result;

    [Given(@"a empresa (.*) nao tem filiais")]
    public void GivenAEmpresaNaoTemFiliais(long companyId)
        => _branchRepository
            .Setup(r => r.GetByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Branch>());

    [Given(@"a empresa (.*) tem a filial ativa ""(.*)""")]
    public void GivenAEmpresaTemAFilialAtiva(long companyId, string name)
    {
        _branches.Add(Branch.Create(companyId, name, null, null, null, null, null, null, null, null).Value);
        _branchRepository
            .Setup(r => r.GetByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_branches.ToArray());
    }

    [Given(@"a empresa (.*) tem a filial inativa ""(.*)""")]
    public void GivenAEmpresaTemAFilialInativa(long companyId, string name)
    {
        var branch = Branch.Create(companyId, name, null, null, null, null, null, null, null, null).Value;
        branch.Deactivate();
        _branches.Add(branch);
        _branchRepository
            .Setup(r => r.GetByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_branches.ToArray());
    }

    [When(@"eu busco as filiais da empresa (.*)")]
    public async Task WhenEuBuscoAsFiliaisDaEmpresa(long companyId)
    {
        var handler = new GetBranchesByCompanyQueryHandler(_branchRepository.Object, _logRepository.Object, _unitOfWork.Object);
        _result = await handler.Handle(new GetBranchesByCompanyQuery(companyId), CancellationToken.None);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();

    [Then(@"a lista de filiais deve estar vazia")]
    public void ThenAListaDeFiliaisDeveEstarVazia()
        => _result!.Value.Should().BeEmpty();

    [Then(@"a lista de filiais deve conter (.*) itens")]
    public void ThenAListaDeFiliaisDeveConterItens(int count)
        => _result!.Value.Should().HaveCount(count);

    [Then(@"a filial ""(.*)"" deve aparecer como ativa na lista")]
    public void ThenAFilialDeveAparecerComoAtivaNaLista(string name)
        => _result!.Value.Single(r => r.Name == name).IsActive.Should().BeTrue();

    [Then(@"a filial ""(.*)"" deve aparecer como inativa na lista")]
    public void ThenAFilialDeveAparecerComoInativaNaLista(string name)
        => _result!.Value.Single(r => r.Name == name).IsActive.Should().BeFalse();
}
