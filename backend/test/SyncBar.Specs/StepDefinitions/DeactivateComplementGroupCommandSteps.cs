using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.DeactivateComplementGroup;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class DeactivateComplementGroupCommandSteps
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
        => SetupGroup(id, companyId, deactivated: false);

    [Given(@"um grupo de complemento inativo com id (.*) da empresa (.*)")]
    public void GivenUmGrupoDeComplementoInativoComIdDaEmpresa(long id, long companyId)
        => SetupGroup(id, companyId, deactivated: true);

    private void SetupGroup(long id, long companyId, bool deactivated)
    {
        _complementGroup = ComplementGroup.Create(companyId, "Grupo Teste", ComplementGroupTypeIds.SelecaoAdicional, 0, 1).Value;
        if (deactivated)
            _complementGroup.Deactivate();

        _complementGroupRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_complementGroup);
    }

    [When(@"eu tento desativar o grupo de complemento (.*)")]
    public async Task WhenEuTentoDesativarOGrupoDeComplemento(long id)
    {
        var handler = new DeactivateComplementGroupCommandHandler(
            _complementGroupRepository.Object, _catalogSyncTrigger.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new DeactivateComplementGroupCommand(id), CancellationToken.None);
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

    [Then(@"o grupo de complemento deve estar inativo")]
    public void ThenOGrupoDeComplementoDeveEstarInativo()
        => _complementGroup!.IsActive.Should().BeFalse();
}
