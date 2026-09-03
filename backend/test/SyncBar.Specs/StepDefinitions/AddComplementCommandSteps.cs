using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.AddComplement;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

// BDD no nivel do handler (Application) — segue o padrao de MarkIfoodOrderReadySteps.cs: os
// colaboradores (repositorios, gatilho de sincronizacao com o Ifood) sao dublados com Moq para
// exercitar o AddComplementCommandHandler real de ponta a ponta.
[Binding]
public sealed class AddComplementCommandSteps
{
    private readonly Mock<IComplementGroupRepository> _complementGroupRepository = new();
    private readonly Mock<IComplementItemRepository> _complementItemRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ComplementGroup? _complementGroup;
    private Result<long>? _result;

    [Given(@"nao existe nenhum grupo de complemento com o id (.*)")]
    public void GivenNaoExisteNenhumGrupoDeComplementoComOId(long id)
        => _complementGroupRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComplementGroup?)null);

    [Given(@"um grupo de complemento ativo com id (.*) da empresa (.*)")]
    public void GivenUmGrupoDeComplementoAtivoComIdDaEmpresa(long id, long companyId)
    {
        _complementGroup = ComplementGroup.Create(companyId, "Grupo Teste", ComplementGroupTypeIds.SelecaoAdicional, 0, 1).Value;

        _complementGroupRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_complementGroup);
    }

    [Given(@"nao existe nenhum item de complemento com o id (.*)")]
    public void GivenNaoExisteNenhumItemDeComplementoComOId(long id)
        => _complementItemRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComplementItem?)null);

    [Given(@"um item de complemento ativo com id (.*) da empresa (.*)")]
    public void GivenUmItemDeComplementoAtivoComIdDaEmpresa(long id, long companyId)
    {
        var complementItem = ComplementItem.Create(companyId, "Item Teste").Value;

        _complementItemRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(complementItem);
    }

    [Given(@"o grupo (.*) ja contem o item de complemento (.*)")]
    public void GivenOGrupoJaContemOItemDeComplemento(long groupId, long itemId)
        => _complementGroup!.AddComplement(itemId, 0m);

    [When(@"eu tento adicionar o item de complemento (.*) ao grupo (.*) com preco extra (.*)")]
    public async Task WhenEuTentoAdicionarOItemDeComplementoAoGrupoComPrecoExtra(long itemId, long groupId, decimal extraPrice)
    {
        var handler = new AddComplementCommandHandler(
            _complementGroupRepository.Object, _complementItemRepository.Object,
            _catalogSyncTrigger.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new AddComplementCommand(groupId, itemId, extraPrice), CancellationToken.None);
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
