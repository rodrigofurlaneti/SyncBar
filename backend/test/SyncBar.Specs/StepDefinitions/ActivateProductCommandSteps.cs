using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.ActivateProduct;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class ActivateProductCommandSteps
{
    private const long CompanyId = 1;

    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Product? _product;
    private Result? _result;

    [Given(@"nao existe nenhum produto com o id (.*)")]
    public void GivenNaoExisteNenhumProdutoComOId(long id)
        => _productRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

    [Given(@"um produto (.*) com id (.*) esta ativo")]
    public void GivenUmProdutoComIdEstaAtivo(string name, long id)
    {
        _product = Product.Create(CompanyId, 1, 1, name, null, null, 10m, null, false, null).Value;
        _productRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_product);
    }

    [Given(@"um produto (.*) com id (.*) esta inativo")]
    public void GivenUmProdutoComIdEstaInativo(string name, long id)
    {
        _product = Product.Create(CompanyId, 1, 1, name, null, null, 10m, null, false, null).Value;
        _product.Deactivate();
        _productRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_product);
    }

    [When(@"eu tento ativar o produto (.*)")]
    public async Task WhenEuTentoAtivarOProduto(long id)
    {
        var handler = new ActivateProductCommandHandler(
            _productRepository.Object, _catalogSyncTrigger.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new ActivateProductCommand(id), CancellationToken.None);
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

    [Then(@"o produto deve continuar ativo")]
    public void ThenOProdutoDeveContinuarAtivo()
        => _product!.IsActive.Should().BeTrue();
}
