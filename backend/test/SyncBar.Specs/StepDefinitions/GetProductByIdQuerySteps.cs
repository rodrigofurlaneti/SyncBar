using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Catalog.GetProductById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class GetProductByIdQuerySteps
{
    private const long CompanyId = 1;

    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<ProductResponse>? _result;

    [Given(@"nao existe nenhum produto com o id (.*) no catalogo")]
    public void GivenNaoExisteNenhumProdutoComOIdNoCatalogo(long id)
        => _productRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

    [Given(@"um produto (.*) com id (.*) esta cadastrado mas inativo no catalogo")]
    public void GivenUmProdutoComIdEstaCadastradoMasInativoNoCatalogo(string name, long id)
    {
        var product = Product.Create(CompanyId, 1, 1, name, null, null, 10m, null, false, null).Value;
        product.Deactivate();
        _productRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
    }

    [Given(@"um produto (.*) com id (.*) e preco (.*) esta cadastrado e ativo no catalogo")]
    public void GivenUmProdutoComIdEPrecoEstaCadastradoEAtivoNoCatalogo(string name, long id, decimal price)
    {
        var product = Product.Create(CompanyId, 1, 1, name, null, null, price, null, false, null).Value;
        _productRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
    }

    [When(@"eu busco o produto pelo id (.*)")]
    public async Task WhenEuBuscoOProdutoPeloId(long id)
    {
        var handler = new GetProductByIdQueryHandler(_productRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetProductByIdQuery(id), CancellationToken.None);
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

    [Then(@"o nome do produto retornado deve ser ""(.*)""")]
    public void ThenONomeDoProdutoRetornadoDeveSer(string name)
        => _result!.Value.Name.Should().Be(name);

    [Then(@"o preco de venda do produto retornado deve ser (.*)")]
    public void ThenOPrecoDeVendaDoProdutoRetornadoDeveSer(decimal price)
        => _result!.Value.SalePrice.Should().Be(price);
}
