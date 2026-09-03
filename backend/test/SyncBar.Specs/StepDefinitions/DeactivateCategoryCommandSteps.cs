using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.DeactivateCategory;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class DeactivateCategoryCommandSteps
{
    private const long CompanyId = 1;

    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Category? _category;
    private Result? _result;

    [Given(@"nao ha nenhuma categoria cadastrada com o id (.*)")]
    public void GivenNaoHaNenhumaCategoriaCadastradaComOId(long id)
        => _categoryRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

    [Given(@"uma categoria (.*) com id (.*) ja esta inativa")]
    public void GivenUmaCategoriaComIdJaEstaInativa(string name, long id)
    {
        _category = Category.Create(CompanyId, name, 0).Value;
        _category.Deactivate();
        _categoryRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_category);
    }

    [Given(@"existe uma categoria ativa (.*) com id (.*)")]
    public void GivenExisteUmaCategoriaAtivaComId(string name, long id)
    {
        _category = Category.Create(CompanyId, name, 0).Value;
        _categoryRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_category);
    }

    [When(@"eu tento desativar a categoria (.*)")]
    public async Task WhenEuTentoDesativarACategoria(long id)
    {
        var handler = new DeactivateCategoryCommandHandler(
            _categoryRepository.Object, _catalogSyncTrigger.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new DeactivateCategoryCommand(id), CancellationToken.None);
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

    [Then(@"a categoria deve estar inativa")]
    public void ThenACategoriaDeveEstarInativa()
        => _category!.IsActive.Should().BeFalse();
}
