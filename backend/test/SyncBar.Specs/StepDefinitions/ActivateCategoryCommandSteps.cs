using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.ActivateCategory;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class ActivateCategoryCommandSteps
{
    private const long CompanyId = 1;

    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Category? _category;
    private Result? _result;

    [Given(@"nao existe nenhuma categoria com o id (.*)")]
    public void GivenNaoExisteNenhumaCategoriaComOId(long id)
        => _categoryRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

    [Given(@"uma categoria (.*) com id (.*) esta ativa")]
    public void GivenUmaCategoriaComIdEstaAtiva(string name, long id)
    {
        _category = Category.Create(CompanyId, name, 0).Value;
        _categoryRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_category);
    }

    [Given(@"uma categoria (.*) com id (.*) esta inativa")]
    public void GivenUmaCategoriaComIdEstaInativa(string name, long id)
    {
        _category = Category.Create(CompanyId, name, 0).Value;
        _category.Deactivate();
        _categoryRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_category);
    }

    [When(@"eu tento ativar a categoria (.*)")]
    public async Task WhenEuTentoAtivarACategoria(long id)
    {
        var handler = new ActivateCategoryCommandHandler(
            _categoryRepository.Object, _catalogSyncTrigger.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new ActivateCategoryCommand(id), CancellationToken.None);
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

    [Then(@"a categoria deve continuar ativa")]
    public void ThenACategoriaDeveContinuarAtiva()
        => _category!.IsActive.Should().BeTrue();
}
