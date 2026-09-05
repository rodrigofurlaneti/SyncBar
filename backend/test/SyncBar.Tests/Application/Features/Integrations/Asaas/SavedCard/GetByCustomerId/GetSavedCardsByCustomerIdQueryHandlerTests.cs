using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId;

public sealed class GetSavedCardsByCustomerIdQueryHandlerTests
{
    private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository = Substitute.For<IAsaasIntegrationSavedCardRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetSavedCardsByCustomerIdQueryHandler _handler;

    public GetSavedCardsByCustomerIdQueryHandlerTests()
    {
        _handler = new GetSavedCardsByCustomerIdQueryHandler(_savedCardRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoCardsForCustomer_ShouldReturnEmptyList()
    {
        _savedCardRepository.GetByCustomerIdAsync(1, Arg.Any<CancellationToken>()).Returns(new List<AsaasIntegrationSavedCard>());

        var result = await _handler.Handle(new GetSavedCardsByCustomerIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_HasCards_ShouldReturnMappedList()
    {
        var cards = new List<AsaasIntegrationSavedCard>
        {
            AsaasIntegrationSavedCard.Create(1, 1, "token-1", "VISA", "1111", isDefault: true).Value,
            AsaasIntegrationSavedCard.Create(1, 1, "token-2", "MASTERCARD", "2222").Value,
        };
        _savedCardRepository.GetByCustomerIdAsync(1, Arg.Any<CancellationToken>()).Returns(cards);

        var result = await _handler.Handle(new GetSavedCardsByCustomerIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(c => c.IsDefault);
    }
}
