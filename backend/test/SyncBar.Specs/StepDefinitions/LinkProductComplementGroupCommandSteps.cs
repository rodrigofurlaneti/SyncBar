using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.LinkProductComplementGroup;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Vincular grupo de complemento a um produto")]
public sealed class LinkProductComplementGroupCommandSteps
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IComplementGroupRepository> _complementGroupRepository = new();
    private readonly Mock<IProductComplementGroupRepository> _productComplementGroupRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<ProductComplementGroup> _existingLinks = [];

    private Result<long>? _result;

    [Given(@"nao existe nenhum produto com o id (.*)")]
    public void GivenNaoExisteNenhumProdutoComOId(long id)
        => _productRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

    [Given(@"um produto ativo com id (.*) da empresa (.*)")]
    public void GivenUmProdutoAtivoComIdDaEmpresa(long id, long companyId)
    {
        var product = Product.Create(companyId, 1, 1, "Produto Teste", null, null, 10m, null, false, null).Value;

        _productRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
    }

    [Given(@"nao existe nenhum grupo de complemento com o id (.*)")]
    public void GivenNaoExisteNenhumGrupoDeComplementoComOId(long id)
        => _complementGroupRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComplementGroup?)null);

    [Given(@"um grupo de complemento ativo com id (.*) da empresa (.*)")]
    public void GivenUmGrupoDeComplementoAtivoComIdDaEmpresa(long id, long companyId)
    {
        var group = ComplementGroup.Create(companyId, "Grupo Teste", ComplementGroupTypeIds.SelecaoAdicional, 0, 1).Value;

        _complementGroupRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
    }

    [Given(@"o produto (.*) ja tem o grupo de complemento (.*) vinculado")]
    public void GivenOProdutoJaTemOGrupoDeComplementoVinculado(long productId, long complementGroupId)
        => _existingLinks.Add(ProductComplementGroup.Create(productId, complementGroupId, 0).Value);

    [When(@"eu tento vincular o grupo de complemento (.*) ao produto (.*) com ordem de exibicao (-?\d+)")]
    public async Task WhenEuTentoVincularOGrupoDeComplementoAoProdutoComOrdemDeExibicao(long complementGroupId, long productId, int displayOrder)
    {
        _productComplementGroupRepository
            .Setup(r => r.GetByProductForUpdateAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingLinks);

        var handler = new LinkProductComplementGroupCommandHandler(
            _productRepository.Object, _complementGroupRepository.Object, _productComplementGroupRepository.Object,
            _catalogSyncTrigger.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new LinkProductComplementGroupCommand(productId, complementGroupId, displayOrder), CancellationToken.None);
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
