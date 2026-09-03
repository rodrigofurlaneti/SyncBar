using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Catalog.Complements;
using SyncBar.Application.Features.Catalog.Complements.GetComplementItems;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Listar itens de complemento de uma empresa")]
public sealed class GetComplementItemsQuerySteps
{
    private readonly Mock<IComplementItemRepository> _complementItemRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<ComplementItem> _items = [];

    private Result<IReadOnlyCollection<ComplementItemResponse>>? _result;

    [Given(@"a empresa (.*) nao possui itens de complemento")]
    public void GivenAEmpresaNaoPossuiItensDeComplemento(long companyId)
        => _complementItemRepository
            .Setup(r => r.GetByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_items);

    [Given(@"um item de complemento com nome ""(.*)"" e ativo (.*) da empresa (.*)")]
    public void GivenUmItemDeComplementoComNomeEAtivoDaEmpresa(string name, bool isActive, long companyId)
    {
        var item = ComplementItem.Create(companyId, name).Value;
        if (!isActive)
            item.Deactivate();

        _items.Add(item);

        _complementItemRepository
            .Setup(r => r.GetByCompanyAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_items);
    }

    [When(@"eu busco os itens de complemento da empresa (.*)")]
    public async Task WhenEuBuscoOsItensDeComplementoDaEmpresa(long companyId)
    {
        var handler = new GetComplementItemsQueryHandler(_complementItemRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new GetComplementItemsQuery(companyId), CancellationToken.None);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();

    [Then(@"a lista de itens de complemento retornada deve ter (.*) itens")]
    public void ThenALstaDeItensDeComplementoRetornadaDeveTerItens(int count)
        => _result!.Value.Count.Should().Be(count);

    [Then(@"o primeiro item da lista deve se chamar ""(.*)""")]
    public void ThenOPrimeiroItemDaListaDeveSeChamar(string name)
        => _result!.Value.First().Name.Should().Be(name);
}
