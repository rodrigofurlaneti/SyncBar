using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Catalog.Complements;
using SyncBar.Application.Features.Catalog.Complements.GetProductComplementGroups;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Listar grupos de complemento vinculados a um produto")]
public sealed class GetProductComplementGroupsQuerySteps
{
    private readonly Mock<IProductComplementGroupRepository> _productComplementGroupRepository = new();
    private readonly Mock<IComplementGroupRepository> _complementGroupRepository = new();
    private readonly Mock<IComplementItemRepository> _complementItemRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<ProductComplementGroup> _links = [];
    private readonly Dictionary<long, ComplementGroup> _groupsById = [];

    private Result<IReadOnlyCollection<ProductComplementGroupResponse>>? _result;

    [Given(@"o produto (.*) nao possui vinculos de grupo de complemento")]
    public void GivenOProdutoNaoPossuiVinculosDeGrupoDeComplemento(long productId)
        => _productComplementGroupRepository
            .Setup(r => r.GetByProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_links);

    [Given(@"um grupo de complemento cadastrado com id (.*) nome ""(.*)"" da empresa (.*)")]
    public void GivenUmGrupoDeComplementoCadastradoComIdNomeDaEmpresa(long id, string name, long companyId)
    {
        var group = ComplementGroup.Create(companyId, name, ComplementGroupTypeIds.SelecaoAdicional, 0, 1).Value;
        _groupsById[id] = group;
    }

    [Given(@"o produto (.*) esta vinculado ao grupo de complemento cadastrado (.*) com ordem de exibicao (.*) no vinculo (.*)")]
    public void GivenOProdutoEstaVinculadoAoGrupoDeComplementoCadastradoComOrdemDeExibicaoNoVinculo(
        long productId, long complementGroupId, int displayOrder, long linkId)
    {
        var link = ProductComplementGroup.Create(productId, complementGroupId, displayOrder).Value;
        _links.Add(link);

        _productComplementGroupRepository
            .Setup(r => r.GetByProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_links);
    }

    [Given(@"o produto (.*) esta vinculado a um grupo de complemento inexistente com ordem de exibicao (.*) no vinculo (.*)")]
    public void GivenOProdutoEstaVinculadoAUmGrupoDeComplementoInexistenteComOrdemDeExibicaoNoVinculo(
        long productId, int displayOrder, long linkId)
    {
        const long orphanGroupId = 999;
        var link = ProductComplementGroup.Create(productId, orphanGroupId, displayOrder).Value;
        _links.Add(link);

        _productComplementGroupRepository
            .Setup(r => r.GetByProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_links);
    }

    [When(@"eu busco os grupos de complemento vinculados ao produto (.*)")]
    public async Task WhenEuBuscoOsGruposDeComplementoVinculadosAoProduto(long productId)
    {
        _complementGroupRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_groupsById.Values.ToList());

        _complementItemRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<ComplementItem>)[]);

        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Product>)[]);

        var handler = new GetProductComplementGroupsQueryHandler(
            _productComplementGroupRepository.Object, _complementGroupRepository.Object, _complementItemRepository.Object,
            _productRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetProductComplementGroupsQuery(productId), CancellationToken.None);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();

    [Then(@"a lista de vinculos retornada deve ter (.*) grupos")]
    public void ThenALstaDeVinculosRetornadaDeveTerGrupos(int count)
        => _result!.Value.Count.Should().Be(count);

    [Then(@"o primeiro vinculo da lista deve se referir ao grupo ""(.*)""")]
    public void ThenOPrimeiroVinculoDaListaDeveSeReferirAoGrupo(string groupName)
        => _result!.Value.First().ComplementGroupName.Should().Be(groupName);
}
