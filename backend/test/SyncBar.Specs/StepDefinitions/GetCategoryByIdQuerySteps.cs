using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Catalog;
using SyncBar.Application.Features.Catalog.GetCategoryById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class GetCategoryByIdQuerySteps
{
    private const long CompanyId = 1;

    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<CategoryResponse>? _result;

    [Given(@"a categoria com id (.*) nao esta cadastrada")]
    public void GivenACategoriaComIdNaoEstaCadastrada(long id)
        => _categoryRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

    [Given(@"uma categoria (.*) com id (.*) esta cadastrada mas inativa")]
    public void GivenUmaCategoriaComIdEstaCadastradaMasInativa(string name, long id)
    {
        var category = Category.Create(CompanyId, name, 0).Value;
        category.Deactivate();
        _categoryRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
    }

    [Given(@"uma categoria (.*) com id (.*), ordem (.*), esta cadastrada e ativa")]
    public void GivenUmaCategoriaComIdOrdemEstaCadastradaEAtiva(string name, long id, int displayOrder)
    {
        var category = Category.Create(CompanyId, name, displayOrder).Value;
        _categoryRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
    }

    [When(@"eu busco a categoria pelo id (.*)")]
    public async Task WhenEuBuscoACategoriaPeloId(long id)
    {
        var handler = new GetCategoryByIdQueryHandler(_categoryRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetCategoryByIdQuery(id), CancellationToken.None);
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

    [Then(@"o nome da categoria retornada deve ser ""(.*)""")]
    public void ThenONomeDaCategoriaRetornadaDeveSer(string name)
        => _result!.Value.Name.Should().Be(name);

    [Then(@"a ordem de exibicao da categoria retornada deve ser (.*)")]
    public void ThenAOrdemDeExibicaoDaCategoriaRetornadaDeveSer(int displayOrder)
        => _result!.Value.DisplayOrder.Should().Be(displayOrder);
}
