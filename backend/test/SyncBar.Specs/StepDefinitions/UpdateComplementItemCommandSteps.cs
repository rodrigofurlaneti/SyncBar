using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementItem;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class UpdateComplementItemCommandSteps
{
    private readonly Mock<IComplementItemRepository> _complementItemRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result? _result;

    [Given(@"nao existe nenhum item de complemento com o id (.*)")]
    public void GivenNaoExisteNenhumItemDeComplementoComOId(long id)
        => _complementItemRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComplementItem?)null);

    [Given(@"um item de complemento ativo com id (.*) da empresa (.*)")]
    public void GivenUmItemDeComplementoAtivoComIdDaEmpresa(long id, long companyId)
    {
        var complementItem = ComplementItem.Create(companyId, "Item Original").Value;

        _complementItemRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(complementItem);
    }

    [When(@"eu tento atualizar o item de complemento (.*) com nome ""(.*)""")]
    public async Task WhenEuTentoAtualizarOItemDeComplementoComNome(long id, string name)
    {
        var handler = new UpdateComplementItemCommandHandler(
            _complementItemRepository.Object, _catalogSyncTrigger.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new UpdateComplementItemCommand(id, name), CancellationToken.None);
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
