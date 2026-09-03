using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Catalog.Complements;
using SyncBar.Application.Features.Catalog.Complements.GetComplementGroups;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class GetComplementGroupsQuerySteps
{
    private readonly Mock<IComplementGroupRepository> _complementGroupRepository = new();
    private readonly Mock<IComplementItemRepository> _complementItemRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<ComplementGroup> _groups = [];
    private readonly Dictionary<long, ComplementItem> _items = [];
    private readonly Dictionary<long, Product> _products = [];

    private Result<IReadOnlyCollection<ComplementGroupResponse>>? _result;

    [Given(@"a empresa (.*) nao possui grupos de complemento")]
    public void GivenAEmpresaNaoPossuiGruposDeComplemento(long companyId)
        => _complementGroupRepository
            .Setup(r => r.GetByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_groups);

    [Given(@"um grupo de complemento ativo com id (.*) nome ""(.*)"" da empresa (.*)")]
    public void GivenUmGrupoDeComplementoAtivoComIdNomeDaEmpresa(long id, string name, long companyId)
    {
        var group = ComplementGroup.Create(companyId, name, ComplementGroupTypeIds.SelecaoAdicional, 0, 1).Value;
        _groups.Add(group);

        _complementGroupRepository
            .Setup(r => r.GetByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_groups);
    }

    [Given(@"o grupo (.*) tem o complemento apontando para o item de complemento (.*) chamado ""(.*)"" com preco extra (.*)")]
    public void GivenOGrupoTemOComplementoApontandoParaOItemDeComplementoChamadoComPrecoExtra(
        long groupIndex, long complementItemId, string itemName, decimal extraPrice)
    {
        var group = _groups[(int)groupIndex - 1];
        var complementItem = ComplementItem.Create(group.CompanyId, itemName).Value;
        _items[complementItemId] = complementItem;

        group.AddComplement(complementItemId, extraPrice);
    }

    [Given(@"o item de complemento (.*) esta vinculado ao produto (.*) com imagem ""(.*)""")]
    public void GivenOItemDeComplementoEstaVinculadoAoProdutoComImagem(long complementItemId, long productId, string imageUrl)
    {
        _items[complementItemId].LinkToProduct(productId);

        var product = Product.Create(1, 1, 1, "Produto Vinculado", null, null, 10m, null, false, null).Value;
        product.SetImage(imageUrl);
        _products[productId] = product;
    }

    [When(@"eu busco os grupos de complemento da empresa (.*)")]
    public async Task WhenEuBuscoOsGruposDeComplementoDaEmpresa(long companyId)
    {
        _complementItemRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_items.Values.ToList());

        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_products.Values.ToList());

        var handler = new GetComplementGroupsQueryHandler(
            _complementGroupRepository.Object, _complementItemRepository.Object, _productRepository.Object,
            _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetComplementGroupsQuery(companyId), CancellationToken.None);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();

    [Then(@"a lista de grupos de complemento retornada deve ter (.*) grupos")]
    public void ThenALstaDeGruposDeComplementoRetornadaDeveTerGrupos(int count)
        => _result!.Value.Count.Should().Be(count);

    [Then(@"o primeiro grupo da lista deve se chamar ""(.*)""")]
    public void ThenOPrimeiroGrupoDaListaDeveSeChamar(string name)
        => _result!.Value.First().Name.Should().Be(name);

    [Then(@"o complemento do item ""(.*)"" deve ter preco extra (.*) e imagem do produto vinculado ""(.*)""")]
    public void ThenOComplementoDoItemDeveTerPrecoExtraEImagemDoProdutoVinculado(string itemName, decimal extraPrice, string imageUrl)
    {
        var complement = _result!.Value
            .SelectMany(g => g.Complements)
            .Single(c => c.ComplementItemName == itemName);

        complement.ExtraPrice.Should().Be(extraPrice);
        complement.LinkedProductImageUrl.Should().Be(imageUrl);
    }
}
