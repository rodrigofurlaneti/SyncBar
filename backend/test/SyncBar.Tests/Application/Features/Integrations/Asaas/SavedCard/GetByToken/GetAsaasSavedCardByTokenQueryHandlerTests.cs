using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByToken;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.GetByToken;

public sealed class GetAsaasSavedCardByTokenQueryHandlerTests
{
    private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository = Substitute.For<IAsaasIntegrationSavedCardRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAsaasSavedCardByTokenQueryHandler _handler;

    public GetAsaasSavedCardByTokenQueryHandlerTests()
    {
        _handler = new GetAsaasSavedCardByTokenQueryHandler(_savedCardRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ShouldReturnNotFound()
    {
        _savedCardRepository.GetByTokenAsync("token-1", Arg.Any<CancellationToken>()).Returns((AsaasIntegrationSavedCard?)null);

        var result = await _handler.Handle(new GetAsaasSavedCardByTokenQuery("token-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSavedCard.NotFound");
    }

    [Fact]
    public async Task Handle_TokenFound_ShouldReturnMappedResponse()
    {
        var card = AsaasIntegrationSavedCard.Create(1, 1, "token-1", "VISA", "1111").Value;
        _savedCardRepository.GetByTokenAsync("token-1", Arg.Any<CancellationToken>()).Returns(card);

        var result = await _handler.Handle(new GetAsaasSavedCardByTokenQuery("token-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be(1);
    }
}
