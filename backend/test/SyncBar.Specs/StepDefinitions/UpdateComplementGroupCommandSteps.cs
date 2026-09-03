using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementGroup;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Atualizar dados de um grupo de complemento")]
public sealed class UpdateComplementGroupCommandSteps
{
    private readonly Mock<IComplementGroupRepository> _complementGroupRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result? _result;

    [Given(@"nao existe nenhum grupo de complemento com o id (.*)")]
    public void GivenNaoExisteNenhumGrupoDeComplementoComOId(long id)
        => _complementGroupRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComplementGroup?)null);

    [Given(@"um grupo de complemento ativo com id (.*) da empresa (.*)")]
    public void GivenUmGrupoDeComplementoAtivoComIdDaEmpresa(long id, long companyId)
    {
        var group = ComplementGroup.Create(companyId, "Grupo Original", ComplementGroupTypeIds.SelecaoAdicional, 0, 1).Value;

        _complementGroupRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
    }

    [When(@"eu tento atualizar o grupo de complemento (.*) com nome ""(.*)"", selecao minima (-?\d+) e selecao maxima (-?\d+)")]
    public async Task WhenEuTentoAtualizarOGrupoDeComplementoComNomeSelecaoMinimaESelecaoMaxima(
        long id, string name, int minSelection, int maxSelection)
    {
        var handler = new UpdateComplementGroupCommandHandler(
            _complementGroupRepository.Object, _catalogSyncTrigger.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new UpdateComplementGroupCommand(id, name, ComplementGroupTypeIds.SelecaoAdicional, minSelection, maxSelection),
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
