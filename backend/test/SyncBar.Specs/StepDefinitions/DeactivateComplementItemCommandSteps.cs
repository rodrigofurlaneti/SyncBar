using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Catalog.Complements.DeactivateComplementItem;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Desativar item de complemento")]
public sealed class DeactivateComplementItemCommandSteps
{
    private readonly Mock<IComplementItemRepository> _complementItemRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ComplementItem? _complementItem;
    private Result? _result;

    [Given(@"nao existe nenhum item de complemento com o id (.*)")]
    public void GivenNaoExisteNenhumItemDeComplementoComOId(long id)
        => _complementItemRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComplementItem?)null);

    [Given(@"um item de complemento ativo com id (.*) da empresa (.*)")]
    public void GivenUmItemDeComplementoAtivoComIdDaEmpresa(long id, long companyId)
        => SetupItem(id, companyId, deactivated: false);

    [Given(@"um item de complemento inativo com id (.*) da empresa (.*)")]
    public void GivenUmItemDeComplementoInativoComIdDaEmpresa(long id, long companyId)
        => SetupItem(id, companyId, deactivated: true);

    private void SetupItem(long id, long companyId, bool deactivated)
    {
        _complementItem = ComplementItem.Create(companyId, "Item Teste").Value;
        if (deactivated)
            _complementItem.Deactivate();

        _complementItemRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_complementItem);
    }

    [When(@"eu tento desativar o item de complemento (.*)")]
    public async Task WhenEuTentoDesativarOItemDeComplemento(long id)
    {
        var handler = new DeactivateComplementItemCommandHandler(
            _complementItemRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new DeactivateComplementItemCommand(id), CancellationToken.None);
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

    [Then(@"o item de complemento deve estar inativo")]
    public void ThenOItemDeComplementoDeveEstarInativo()
        => _complementItem!.IsActive.Should().BeFalse();
}
