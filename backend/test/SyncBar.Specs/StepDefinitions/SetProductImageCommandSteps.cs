using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Storage;
using SyncBar.Application.Features.Catalog.SetProductImage;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Definir imagem do produto")]
public sealed class SetProductImageCommandSteps
{
    private const long CompanyId = 1;

    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IImageStorage> _imageStorage = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Product? _product;
    private Result<string>? _result;

    [Given(@"nao existe nenhum produto para definir imagem com o id (.*)")]
    public void GivenNaoExisteNenhumProdutoParaDefinirImagemComOId(long id)
        => _productRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

    [Given(@"um produto (.*) com id (.*) esta inativo e sem imagem")]
    public void GivenUmProdutoComIdEstaInativoESemImagem(string name, long id)
    {
        _product = Product.Create(CompanyId, 1, 1, name, null, null, 10m, null, false, null).Value;
        _product.Deactivate();
        _productRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_product);
    }

    [Given(@"um produto (.*) com id (.*) esta ativo e sem imagem")]
    public void GivenUmProdutoComIdEstaAtivoESemImagem(string name, long id)
    {
        _product = Product.Create(CompanyId, 1, 1, name, null, null, 10m, null, false, null).Value;
        _productRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_product);
    }

    [Given(@"o armazenamento de imagens salva a imagem do produto e retorna a url ""(.*)""")]
    public void GivenOArmazenamentoDeImagensSalvaAImagemDoProdutoERetornaAUrl(string url)
        => _imageStorage
            .Setup(s => s.SaveProductImageAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(url);

    [When(@"eu tento definir a imagem do produto (.*) com extensao ""(.*)""")]
    public async Task WhenEuTentoDefinirAImagemDoProdutoComExtensao(long id, string extension)
    {
        var handler = new SetProductImageCommandHandler(
            _productRepository.Object, _imageStorage.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new SetProductImageCommand(id, extension, [1, 2, 3]), CancellationToken.None);
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

    [Then(@"a url da imagem retornada deve ser ""(.*)""")]
    public void ThenAUrlDaImagemRetornadaDeveSer(string url)
        => _result!.Value.Should().Be(url);

    [Then(@"a imagem do produto deve ser ""(.*)""")]
    public void ThenAImagemDoProdutoDeveSer(string url)
        => _product!.ImageUrl.Should().Be(url);
}
