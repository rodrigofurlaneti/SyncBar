using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Catalog;
using SyncBar.Application.Features.Catalog.GetMenuForManagement;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class GetMenuForManagementQuerySteps
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<Product> _products = [];
    private Result<IReadOnlyCollection<ProductManagementResponse>>? _result;

    [Given(@"um produto inativo ""(.*)"" com id (.*), categoria (.*) e preco (.*) esta cadastrado na empresa (.*) para gerenciamento")]
    public void GivenUmProdutoInativoComIdCategoriaEPrecoEstaCadastradoNaEmpresaParaGerenciamento(
        string name, long productId, long categoryId, decimal price, long companyId)
    {
        var product = Product.Create(companyId, categoryId, 1, name, null, null, price, null, false, null).Value;
        product.Deactivate();
        _products.Add(product);
        SetupRepositories(companyId);
    }

    [Given(@"um produto ativo ""(.*)"" com id (.*), categoria (.*) e preco (.*) esta cadastrado na empresa (.*) para gerenciamento")]
    public void GivenUmProdutoAtivoComIdCategoriaEPrecoEstaCadastradoNaEmpresaParaGerenciamento(
        string name, long productId, long categoryId, decimal price, long companyId)
    {
        var product = Product.Create(companyId, categoryId, 1, name, null, null, price, null, false, null).Value;
        _products.Add(product);
        SetupRepositories(companyId);
    }

    private void SetupRepositories(long companyId)
    {
        _productRepository
            .Setup(r => r.GetAllByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Product>)_products.AsReadOnly());

        // Nenhuma categoria cadastrada de proposito — cobre o fallback "Categoria removida" e o
        // estado ja desativado dos produtos, sem depender do mapa de categorias.
        _categoryRepository
            .Setup(r => r.GetAllByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Category>)Array.Empty<Category>());
    }

    [When(@"eu busco o cardapio de gerenciamento da empresa (.*)")]
    public async Task WhenEuBuscoOCardapioDeGerenciamentoDaEmpresa(long companyId)
    {
        var handler = new GetMenuForManagementQueryHandler(
            _productRepository.Object, _categoryRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetMenuForManagementQuery(companyId), CancellationToken.None);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();

    [Then(@"o produto ""(.*)"" na lista de gerenciamento do cardapio deve estar inativo")]
    public void ThenOProdutoNaListaDeGerenciamentoDoCardapioDeveEstarInativo(string name)
        => _result!.Value.Single(p => p.Name == name).IsActive.Should().BeFalse();

    [Then(@"o nome da categoria do produto ""(.*)"" na lista de gerenciamento deve ser ""(.*)""")]
    public void ThenONomeDaCategoriaDoProdutoNaListaDeGerenciamentoDeveSer(string productName, string categoryName)
        => _result!.Value.Single(p => p.Name == productName).CategoryName.Should().Be(categoryName);
}
