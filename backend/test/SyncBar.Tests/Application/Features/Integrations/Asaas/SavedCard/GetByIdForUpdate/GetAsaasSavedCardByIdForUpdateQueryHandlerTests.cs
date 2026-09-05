using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByIdForUpdate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.GetByIdForUpdate;

public sealed class GetAsaasSavedCardByIdForUpdateQueryHandlerTests
{
    private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository = Substitute.For<IAsaasIntegrationSavedCardRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAsaasSavedCardByIdForUpdateQueryHandler _handler;

    public GetAsaasSavedCardByIdForUpdateQueryHandlerTests()
    {
        _handler = new GetAsaasSavedCardByIdForUpdateQueryHandler(_savedCardRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_CardNotFound_ShouldReturnNotFound()
    {
        _savedCardRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationSavedCard?)null);

        var result = await _handler.Handle(new GetAsaasSavedCardByIdForUpdateQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSavedCard.NotFound");
    }

    [Fact]
    public async Task Handle_CardFound_ShouldReturnMappedResponse()
    {
        var card = AsaasIntegrationSavedCard.Create(1, 1, "token", "VISA", "1111").Value;
        _savedCardRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(card);

        var result = await _handler.Handle(new GetAsaasSavedCardByIdForUpdateQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CardBrand.Should().Be("VISA");
    }
}
