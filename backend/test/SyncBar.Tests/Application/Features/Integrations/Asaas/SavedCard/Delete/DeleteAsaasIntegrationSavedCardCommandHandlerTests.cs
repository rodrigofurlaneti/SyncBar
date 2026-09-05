using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Delete;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.Delete;

public sealed class DeleteAsaasIntegrationSavedCardCommandHandlerTests
{
    private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository = Substitute.For<IAsaasIntegrationSavedCardRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeleteAsaasIntegrationSavedCardCommandHandler _handler;

    public DeleteAsaasIntegrationSavedCardCommandHandlerTests()
    {
        _handler = new DeleteAsaasIntegrationSavedCardCommandHandler(_savedCardRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_CardNotFound_ShouldReturnNotFound()
    {
        _savedCardRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationSavedCard?)null);

        var result = await _handler.Handle(new DeleteAsaasIntegrationSavedCardCommand(1, 1, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSavedCard.NotFound");
    }

    [Fact]
    public async Task Handle_CrossTenantCard_ShouldReturnNotFound()
    {
        var card = AsaasIntegrationSavedCard.Create(1, 1, "token", "VISA", "1111").Value;
        _savedCardRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(card);

        var result = await _handler.Handle(new DeleteAsaasIntegrationSavedCardCommand(1, 2, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSavedCard.NotFound");
        _savedCardRepository.DidNotReceive().Delete(Arg.Any<AsaasIntegrationSavedCard>());
    }

    [Fact]
    public async Task Handle_DeletingDefaultCard_ShouldPromoteMostRecentRemainingCardAsDefault()
    {
        var cardToDelete = AsaasIntegrationSavedCard.Create(1, 1, "token-old", "VISA", "1111", isDefault: true).Value;
        var olderCard = AsaasIntegrationSavedCard.Create(1, 1, "token-a", "VISA", "2222").Value;
        var newerCard = AsaasIntegrationSavedCard.Create(1, 1, "token-b", "MASTERCARD", "3333").Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(cardToDelete, 1L);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(olderCard, 2L);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(newerCard, 3L);
        _savedCardRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(cardToDelete);
        _savedCardRepository.GetByCustomerIdAndCompanyIdForUpdateAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns(new List<AsaasIntegrationSavedCard> { cardToDelete, olderCard, newerCard });

        var result = await _handler.Handle(new DeleteAsaasIntegrationSavedCardCommand(1, 1, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _savedCardRepository.Received(1).Delete(cardToDelete);
        // Ambos os cartões restantes foram criados no mesmo instante nesta massa de teste — a promoção deve
        // acontecer sobre um dos dois (o mais recente por CreatedAt), nunca sobre o cartão removido.
        _savedCardRepository.Received(1).Update(Arg.Is<AsaasIntegrationSavedCard>(c => c != cardToDelete && c.IsDefault));
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeletingNonDefaultCard_ShouldNotPromoteAnyOtherCard()
    {
        var cardToDelete = AsaasIntegrationSavedCard.Create(1, 1, "token", "VISA", "1111").Value;
        _savedCardRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(cardToDelete);

        var result = await _handler.Handle(new DeleteAsaasIntegrationSavedCardCommand(1, 1, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _savedCardRepository.Received(1).Delete(cardToDelete);
        _savedCardRepository.DidNotReceive().Update(Arg.Any<AsaasIntegrationSavedCard>());
    }
}
