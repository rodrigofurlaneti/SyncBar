using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.Update;

public sealed class UpdateAsaasIntegrationSavedCardCommandHandlerTests
{
    private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository = Substitute.For<IAsaasIntegrationSavedCardRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateAsaasIntegrationSavedCardCommandHandler _handler;

    public UpdateAsaasIntegrationSavedCardCommandHandlerTests()
    {
        _handler = new UpdateAsaasIntegrationSavedCardCommandHandler(_savedCardRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_CardNotFound_ShouldReturnNotFound()
    {
        _savedCardRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationSavedCard?)null);

        var result = await _handler.Handle(new UpdateAsaasIntegrationSavedCardCommand(1, 1, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSavedCard.NotFound");
    }

    [Fact]
    public async Task Handle_CrossTenantCard_ShouldReturnNotFound()
    {
        var card = AsaasIntegrationSavedCard.Create(1, 1, "token", "VISA", "1111").Value;
        _savedCardRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(card);

        var result = await _handler.Handle(new UpdateAsaasIntegrationSavedCardCommand(1, 2, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSavedCard.NotFound");
    }

    [Fact]
    public async Task Handle_SetAsDefaultTrue_ShouldUnsetOtherDefaultCardsAndMarkThisOne()
    {
        var card = AsaasIntegrationSavedCard.Create(1, 1, "token", "VISA", "1111").Value;
        var otherDefault = AsaasIntegrationSavedCard.Create(1, 1, "other-token", "MASTERCARD", "2222", isDefault: true).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(card, 1L);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(otherDefault, 2L);
        _savedCardRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(card);
        _savedCardRepository.GetByCustomerIdAndCompanyIdForUpdateAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns(new List<AsaasIntegrationSavedCard> { card, otherDefault });

        var result = await _handler.Handle(new UpdateAsaasIntegrationSavedCardCommand(1, 1, 1, SetAsDefault: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        card.IsDefault.Should().BeTrue();
        otherDefault.IsDefault.Should().BeFalse();
        _savedCardRepository.Received(1).Update(otherDefault);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetAsDefaultFalse_ShouldRemoveDefaultFlagFromCard()
    {
        var card = AsaasIntegrationSavedCard.Create(1, 1, "token", "VISA", "1111", isDefault: true).Value;
        _savedCardRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(card);

        var result = await _handler.Handle(new UpdateAsaasIntegrationSavedCardCommand(1, 1, 1, SetAsDefault: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        card.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateHolderAndExpiry()
    {
        var card = AsaasIntegrationSavedCard.Create(1, 1, "token", "VISA", "1111", holderName: "Old Name").Value;
        _savedCardRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(card);

        var result = await _handler.Handle(
            new UpdateAsaasIntegrationSavedCardCommand(1, 1, 1, HolderName: "New Name", ExpiryMonth: "01", ExpiryYear: "2031"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        card.HolderName.Should().Be("New Name");
        card.ExpiryMonth.Should().Be("01");
        card.ExpiryYear.Should().Be("2031");
        _savedCardRepository.Received(1).Update(card);
    }
}
