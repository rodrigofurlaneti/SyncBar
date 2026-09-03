using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.CreateCategory;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Criar categoria")]
public sealed class CreateCategoryCommandSteps
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<long>? _result;

    [When(@"eu tento criar a categoria com nome vazio, ordem (.*), para a empresa (.*)")]
    public async Task WhenEuTentoCriarACategoriaComNomeVazioOrdemParaAEmpresa(int displayOrder, long companyId)
        => await CreateCategoryAsync(string.Empty, displayOrder, companyId);

    [When(@"eu tento criar a categoria ""(.*)"" com ordem (.*) para a empresa (.*)")]
    public async Task WhenEuTentoCriarACategoriaComOrdemParaAEmpresa(string name, int displayOrder, long companyId)
        => await CreateCategoryAsync(name, displayOrder, companyId);

    private async Task CreateCategoryAsync(string name, int displayOrder, long companyId)
    {
        var handler = new CreateCategoryCommandHandler(
            _categoryRepository.Object, _catalogSyncTrigger.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new CreateCategoryCommand(companyId, name, displayOrder), CancellationToken.None);
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

    [Then(@"a categoria criada deve ser adicionada ao repositorio da empresa (.*)")]
    public void ThenACategoriaCriadaDeveSerAdicionadaAoRepositorioDaEmpresa(long companyId)
        => _categoryRepository.Verify(
            r => r.AddAsync(It.Is<Category>(c => c.CompanyId == companyId), It.IsAny<CancellationToken>()),
            Times.Once);
}
