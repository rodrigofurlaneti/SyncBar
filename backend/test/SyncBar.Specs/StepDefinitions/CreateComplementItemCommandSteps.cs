using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Tenancy;
using SyncBar.Application.Features.Catalog.Complements.CreateComplementItem;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class CreateComplementItemCommandSteps
{
    private readonly Mock<IComplementItemRepository> _complementItemRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICurrentTenantService> _currentTenant = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<long>? _result;

    [Given(@"o tenant autenticado nao possui empresa")]
    public void GivenOTenantAutenticadoNaoPossuiEmpresa()
        => _currentTenant.Setup(t => t.CompanyId).Returns((long?)null);

    [Given(@"o tenant autenticado pertence a empresa (.*)")]
    public void GivenOTenantAutenticadoPertenceAEmpresa(long companyId)
        => _currentTenant.Setup(t => t.CompanyId).Returns(companyId);

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

    [When(@"eu tento criar um item de complemento para a empresa (.*) com nome ""(.*)""")]
    public async Task WhenEuTentoCriarUmItemDeComplementoParaAEmpresaComNome(long companyId, string name)
        => await ExecuteAsync(companyId, name, null);

    [When(@"eu tento criar um item de complemento para a empresa (.*) com nome ""(.*)"" vinculado ao produto (.*)")]
    public async Task WhenEuTentoCriarUmItemDeComplementoParaAEmpresaComNomeVinculadoAoProduto(long companyId, string name, long linkedProductId)
        => await ExecuteAsync(companyId, name, linkedProductId);

    private async Task ExecuteAsync(long companyId, string name, long? linkedProductId)
    {
        var handler = new CreateComplementItemCommandHandler(
            _complementItemRepository.Object, _productRepository.Object, _currentTenant.Object,
            _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new CreateComplementItemCommand(companyId, name, linkedProductId), CancellationToken.None);
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
