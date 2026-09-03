using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.UpdateProduct;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class UpdateProductCommandSteps
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Product? _product;
    private Result? _result;

    [Given(@"nao existe nenhum produto para atualizar com o id (.*)")]
    public void GivenNaoExisteNenhumProdutoParaAtualizarComOId(long id)
        => _productRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

    [Given(@"um produto (.*) com id (.*) esta ativo e pertence a empresa (.*)")]
    public void GivenUmProdutoComIdEstaAtivoEPertenceAEmpresa(string name, long id, long companyId)
    {
        _product = Product.Create(companyId, 1, 1, name, null, null, 10m, null, false, null).Value;
        _productRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_product);
    }

    [Given(@"nao existe categoria alguma com o id (.*) para atualizacao de produto")]
    public void GivenNaoExisteCategoriaAlgumaComOIdParaAtualizacaoDeProduto(long categoryId)
        => _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

    [Given(@"a categoria (.*) com id (.*) esta ativa e pertence a empresa (.*)")]
    public void GivenACategoriaComIdEstaAtivaEPertenceAEmpresa(string name, long categoryId, long companyId)
    {
        var category = Category.Create(companyId, name, 0).Value;
        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
    }

    [When(@"eu tento atualizar o produto (.*) na categoria (.*) com preco (.*)")]
    public async Task WhenEuTentoAtualizarOProdutoNaCategoriaComPreco(long productId, long categoryId, decimal salePrice)
    {
        var handler = new UpdateProductCommandHandler(
            _productRepository.Object, _categoryRepository.Object, _catalogSyncTrigger.Object,
            _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new UpdateProductCommand(productId, categoryId, 1, "Produto atualizado", null, null, salePrice, null, false, null),
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

    [Then(@"o preco de venda do produto atualizado deve ser (.*)")]
    public void ThenOPrecoDeVendaDoProdutoAtualizadoDeveSer(decimal salePrice)
        => _product!.SalePrice.Should().Be(salePrice);

    [Then(@"a categoria do produto atualizado deve ser (.*)")]
    public void ThenACategoriaDoProdutoAtualizadoDeveSer(long categoryId)
        => _product!.CategoryId.Should().Be(categoryId);
}
