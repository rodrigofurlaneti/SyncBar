using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Catalog;
using SyncBar.Application.Features.Catalog.GetCategoriesForManagement;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Consultar categorias para gerenciamento")]
public sealed class GetCategoriesForManagementQuerySteps
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<Category> _categories = [];
    private readonly List<Product> _products = [];
    private Result<IReadOnlyCollection<CategoryManagementResponse>>? _result;

    [Given(@"a empresa (.*) nao possui nenhuma categoria cadastrada para gerenciamento")]
    public void GivenAEmpresaNaoPossuiNenhumaCategoriaCadastradaParaGerenciamento(long companyId)
        => SetupRepositories(companyId);

    [Given(@"a categoria inativa ""(.*)"" com id (.*) e ordem (.*) pertence a empresa (.*)")]
    public void GivenACategoriaInativaComIdEOrdemPertenceAEmpresa(string name, long id, int displayOrder, long companyId)
    {
        var category = Category.Create(companyId, name, displayOrder).Value;
        category.Deactivate();
        _categories.Add(category);
        SetupRepositories(companyId);
    }

    [Given(@"a categoria ativa ""(.*)"" com id (.*) e ordem (.*) esta cadastrada na empresa (.*) para gerenciamento")]
    public void GivenACategoriaAtivaComIdEOrdemEstaCadastradaNaEmpresaParaGerenciamento(string name, long id, int displayOrder, long companyId)
    {
        _categories.Add(Category.Create(companyId, name, displayOrder).Value);
        SetupRepositories(companyId);
    }

    [Given(@"existem (.*) produtos cadastrados na categoria (.*)")]
    public void GivenExistemProdutosCadastradosNaCategoria(int count, long categoryId)
    {
        for (var i = 0; i < count; i++)
        {
            var product = Product.Create(1, categoryId, 1, $"Produto {i}", null, null, 10m, null, false, null).Value;
            _products.Add(product);
        }

        SetupRepositories(1);
    }

    private void SetupRepositories(long companyId)
    {
        _categoryRepository
            .Setup(r => r.GetAllByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Category>)_categories.AsReadOnly());

        _productRepository
            .Setup(r => r.GetAllByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Product>)_products.AsReadOnly());
    }

    [When(@"eu busco as categorias para gerenciamento da empresa (.*)")]
    public async Task WhenEuBuscoAsCategoriasParaGerenciamentoDaEmpresa(long companyId)
    {
        var handler = new GetCategoriesForManagementQueryHandler(
            _categoryRepository.Object, _productRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetCategoriesForManagementQuery(companyId), CancellationToken.None);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();

    [Then(@"a lista de categorias para gerenciamento deve conter (.*) categorias")]
    public void ThenAListaDeCategoriasParaGerenciamentoDeveConterCategorias(int count)
        => _result!.Value.Should().HaveCount(count);

    [Then(@"a categoria ""(.*)"" na lista de gerenciamento deve estar inativa")]
    public void ThenACategoriaNaListaDeGerenciamentoDeveEstarInativa(string name)
        => _result!.Value.Single(c => c.Name == name).IsActive.Should().BeFalse();

    [Then(@"a categoria ""(.*)"" na lista de gerenciamento deve ter (.*) produtos")]
    public void ThenACategoriaNaListaDeGerenciamentoDeveTerProdutos(string name, int count)
        => _result!.Value.Single(c => c.Name == name).ProductCount.Should().Be(count);
}
