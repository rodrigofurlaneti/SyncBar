using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.UnlinkProductComplementGroup;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Desvincular grupo de complemento de um produto")]
public sealed class UnlinkProductComplementGroupCommandSteps
{
    private readonly Mock<IProductComplementGroupRepository> _productComplementGroupRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result? _result;

    [Given(@"nao existe nenhum vinculo produto-grupo com o id (.*)")]
    public void GivenNaoExisteNenhumVinculoProdutoGrupoComOId(long id)
        => _productComplementGroupRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductComplementGroup?)null);

    [Given(@"um vinculo produto-grupo ativo com id (.*) do produto (.*) e grupo (.*)")]
    public void GivenUmVinculoProdutoGrupoAtivoComIdDoProdutoEGrupo(long id, long productId, long complementGroupId)
    {
        var link = ProductComplementGroup.Create(productId, complementGroupId, 0).Value;

        _productComplementGroupRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);
    }

    [Given(@"um produto ativo com id (.*) da empresa (.*)")]
    public void GivenUmProdutoAtivoComIdDaEmpresa(long id, long companyId)
    {
        var product = Product.Create(companyId, 1, 1, "Produto Teste", null, null, 10m, null, false, null).Value;

        _productRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
    }

    [Given(@"nao existe nenhum produto com o id (.*)")]
    public void GivenNaoExisteNenhumProdutoComOId(long id)
        => _productRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

    [When(@"eu tento desvincular o vinculo produto-grupo (.*)")]
    public async Task WhenEuTentoDesvincularOVinculoProdutoGrupo(long id)
    {
        var handler = new UnlinkProductComplementGroupCommandHandler(
            _productComplementGroupRepository.Object, _productRepository.Object,
            _catalogSyncTrigger.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new UnlinkProductComplementGroupCommand(id), CancellationToken.None);
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
}
