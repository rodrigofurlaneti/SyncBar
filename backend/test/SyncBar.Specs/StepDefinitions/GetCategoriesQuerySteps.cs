using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Catalog;
using SyncBar.Application.Features.Catalog.GetCategories;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class GetCategoriesQuerySteps
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<Category> _categories = [];
    private Result<IReadOnlyCollection<CategoryResponse>>? _result;

    [Given(@"a empresa (.*) nao possui nenhuma categoria ativa")]
    public void GivenAEmpresaNaoPossuiNenhumaCategoriaAtiva(long companyId)
        => _categoryRepository
            .Setup(r => r.GetByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Category>)_categories.AsReadOnly());

    [Given(@"a categoria ativa ""(.*)"" com id (.*) e ordem (.*) pertence a empresa (.*)")]
    public void GivenACategoriaAtivaComIdEOrdemPertenceAEmpresa(string name, long id, int displayOrder, long companyId)
    {
        _categories.Add(Category.Create(companyId, name, displayOrder).Value);

        _categoryRepository
            .Setup(r => r.GetByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Category>)_categories.AsReadOnly());
    }

    [When(@"eu busco as categorias ativas da empresa (.*)")]
    public async Task WhenEuBuscoAsCategoriasAtivasDaEmpresa(long companyId)
    {
        var handler = new GetCategoriesQueryHandler(_categoryRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetCategoriesQuery(companyId), CancellationToken.None);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();

    [Then(@"a lista de categorias retornada deve estar vazia")]
    public void ThenAListaDeCategoriasRetornadaDeveEstarVazia()
        => _result!.Value.Should().BeEmpty();

    [Then(@"a lista de categorias retornada deve conter (.*) categorias")]
    public void ThenAListaDeCategoriasRetornadaDeveConterCategorias(int count)
        => _result!.Value.Should().HaveCount(count);

    [Then(@"a categoria na posicao (.*) da lista deve ser ""(.*)""")]
    public void ThenACategoriaNaPosicaoDaListaDeveSer(int index, string name)
        => _result!.Value.ElementAt(index).Name.Should().Be(name);
}
