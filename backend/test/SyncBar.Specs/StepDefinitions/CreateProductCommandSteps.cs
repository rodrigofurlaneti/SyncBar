using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.CreateProduct;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Criar produto")]
public sealed class CreateProductCommandSteps
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<long>? _result;

    [Given(@"nao existe nenhuma categoria cadastrada com o id (.*)")]
    public void GivenNaoExisteNenhumaCategoriaCadastradaComOId(long categoryId)
        => _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

    [Given(@"a categoria (.*) com id (.*) esta inativa para a empresa (.*)")]
    public void GivenACategoriaComIdEstaInativaParaAEmpresa(string name, long categoryId, long companyId)
    {
        var category = Category.Create(companyId, name, 0).Value;
        category.Deactivate();
        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
    }

    [Given(@"a categoria (.*) com id (.*) esta ativa para a empresa (.*)")]
    public void GivenACategoriaComIdEstaAtivaParaAEmpresa(string name, long categoryId, long companyId)
    {
        var category = Category.Create(companyId, name, 0).Value;
        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
    }

    [When(@"eu tento criar o produto ""(.*)"" na categoria (.*) para a empresa (.*)")]
    public async Task WhenEuTentoCriarOProdutoNaCategoriaParaAEmpresa(string name, long categoryId, long companyId)
    {
        var handler = new CreateProductCommandHandler(
            _productRepository.Object, _categoryRepository.Object, _catalogSyncTrigger.Object,
            _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new CreateProductCommand(companyId, categoryId, 1, name, null, null, 10m, null, false, null),
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
}
