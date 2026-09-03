using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Catalog;
using SyncBar.Application.Features.Catalog.GetMenu;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Consultar cardapio")]
public sealed class GetMenuQuerySteps
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IProductComplementGroupRepository> _productComplementGroupRepository = new();
    private readonly Mock<IComplementGroupRepository> _complementGroupRepository = new();
    private readonly Mock<IComplementItemRepository> _complementItemRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<Product> _products = [];
    private readonly List<Category> _categories = [];
    private Result<IReadOnlyCollection<MenuItemResponse>>? _result;

    [Given(@"a empresa (.*) nao possui nenhum produto no cardapio")]
    public void GivenAEmpresaNaoPossuiNenhumProdutoNoCardapio(long companyId)
        => SetupRepositories(companyId);

    [Given(@"a categoria (.*) com nome ""(.*)"" pertence a empresa (.*)")]
    public void GivenACategoriaComNomePertenceAEmpresa(long categoryId, string name, long companyId)
    {
        _categories.Add(Category.Create(companyId, name, 0).Value);
        SetupRepositories(companyId);
    }

    [Given(@"um produto ativo ""(.*)"" com id (.*), categoria (.*) e preco (.*) pertence a empresa (.*)")]
    public void GivenUmProdutoAtivoComIdCategoriaEPrecoPertenceAEmpresa(
        string name, long productId, long categoryId, decimal price, long companyId)
    {
        var product = Product.Create(companyId, categoryId, 1, name, null, null, price, null, false, null).Value;
        _products.Add(product);

        // Sem vinculos de complemento cadastrados — GetByProductsAsync retorna vazio e o
        // MenuComplementsBuilder encerra cedo (links.Count == 0), sem precisar acionar os demais
        // repositorios de complemento.
        _productComplementGroupRepository
            .Setup(r => r.GetByProductsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<ProductComplementGroup>)Array.Empty<ProductComplementGroup>());

        SetupRepositories(companyId);
    }

    private void SetupRepositories(long companyId)
    {
        _productRepository
            .Setup(r => r.GetByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Product>)_products.AsReadOnly());

        _categoryRepository
            .Setup(r => r.GetByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Category>)_categories.AsReadOnly());
    }

    [When(@"eu busco o cardapio da empresa (.*)")]
    public async Task WhenEuBuscoOCardapioDaEmpresa(long companyId)
    {
        var handler = new GetMenuQueryHandler(
            _productRepository.Object, _categoryRepository.Object, _productComplementGroupRepository.Object,
            _complementGroupRepository.Object, _complementItemRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetMenuQuery(companyId), CancellationToken.None);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();

    [Then(@"a lista do cardapio deve estar vazia")]
    public void ThenAListaDoCardapioDeveEstarVazia()
        => _result!.Value.Should().BeEmpty();

    [Then(@"a lista do cardapio deve conter (.*) item")]
    public void ThenAListaDoCardapioDeveConterItem(int count)
        => _result!.Value.Should().HaveCount(count);

    [Then(@"o nome da categoria do item na posicao (.*) do cardapio deve ser ""(.*)""")]
    public void ThenONomeDaCategoriaDoItemNaPosicaoDoCardapioDeveSer(int index, string categoryName)
        => _result!.Value.ElementAt(index).CategoryName.Should().Be(categoryName);
}
