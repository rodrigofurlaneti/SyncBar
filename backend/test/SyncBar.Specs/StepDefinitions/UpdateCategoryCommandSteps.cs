using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.UpdateCategory;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Atualizar categoria")]
public sealed class UpdateCategoryCommandSteps
{
    private const long CompanyId = 1;

    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Category? _category;
    private Result? _result;

    [Given(@"nao existe nenhuma categoria para atualizar com o id (.*)")]
    public void GivenNaoExisteNenhumaCategoriaParaAtualizarComOId(long id)
        => _categoryRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

    [Given(@"uma categoria (.*) com id (.*) esta cadastrada e ativa para atualizacao")]
    public void GivenUmaCategoriaComIdEstaCadastradaEAtivaParaAtualizacao(string name, long id)
    {
        _category = Category.Create(CompanyId, name, 0).Value;
        _categoryRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_category);
    }

    [When(@"eu tento atualizar a categoria (.*) para o nome ""(.*)"" e ordem (.*)")]
    public async Task WhenEuTentoAtualizarACategoriaParaONomeEOrdem(long id, string name, int displayOrder)
    {
        var handler = new UpdateCategoryCommandHandler(
            _categoryRepository.Object, _catalogSyncTrigger.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new UpdateCategoryCommand(id, name, displayOrder), CancellationToken.None);
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

    [Then(@"o nome da categoria deve ser ""(.*)""")]
    public void ThenONomeDaCategoriaDeveSer(string name)
        => _category!.Name.Should().Be(name);

    [Then(@"a ordem de exibicao da categoria deve ser (.*)")]
    public void ThenAOrdemDeExibicaoDaCategoriaDeveSer(int displayOrder)
        => _category!.DisplayOrder.Should().Be(displayOrder);
}
