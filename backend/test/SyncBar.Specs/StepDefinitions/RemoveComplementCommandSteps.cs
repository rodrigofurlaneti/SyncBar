using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.RemoveComplement;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Remover opcao de um grupo de complemento")]
public sealed class RemoveComplementCommandSteps
{
    private readonly Mock<IComplementGroupRepository> _complementGroupRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ComplementGroup? _complementGroup;
    private Result? _result;

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

    [Given(@"o grupo (.*) tem o complemento (.*) apontando para o item de complemento (.*)")]
    public void GivenOGrupoTemOComplementoApontandoParaOItemDeComplemento(long groupId, long complementId, long complementItemId)
        => _complementGroup!.AddComplement(complementItemId, 0m);

    [When(@"eu tento remover o complemento (.*) do grupo (.*)")]
    public async Task WhenEuTentoRemoverOComplementoDoGrupo(long complementId, long groupId)
    {
        // O id real do Complement recem-adicionado e sempre 0 em memoria (Entity criada via
        // base(0) — nunca persistida por um DbContext real neste teste). Resolve o id efetivo do
        // unico complemento ativo do grupo quando houver, senao usa o id informado no cenario
        // (que nao existira, gerando ComplementGroup.ComplementNotFound como esperado).
        var effectiveComplementId = _complementGroup?.Complements.FirstOrDefault(c => c.IsActive)?.Id ?? complementId;

        var handler = new RemoveComplementCommandHandler(
            _complementGroupRepository.Object, _catalogSyncTrigger.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new RemoveComplementCommand(groupId, effectiveComplementId), CancellationToken.None);
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
