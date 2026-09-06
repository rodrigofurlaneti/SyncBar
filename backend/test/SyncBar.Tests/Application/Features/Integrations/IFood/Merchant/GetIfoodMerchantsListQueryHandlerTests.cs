using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Merchant;

public sealed class GetIfoodMerchantsListQueryHandlerTests
{
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodMerchantClient _merchantClient = Substitute.For<IIfoodMerchantClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodMerchantsListQueryHandler _handler;

    public GetIfoodMerchantsListQueryHandlerTests()
    {
        _handler = new GetIfoodMerchantsListQueryHandler(_tokenProvider, _merchantClient, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoToken_ShouldReturnNotConnected()
    {
        var query = new GetIfoodMerchantsListQuery(1);
        _tokenProvider.GetAccessTokenAsync(1, Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.NotConnected");
    }

    [Fact]
    public async Task Handle_IfoodApiFails_ShouldReturnListFailed()
    {
        var query = new GetIfoodMerchantsListQuery(1);
        _tokenProvider.GetAccessTokenAsync(1, Arg.Any<CancellationToken>()).Returns("token-1");
        _merchantClient.ListMerchantsAsync("token-1", 1, 100, Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantListResult(false, [], "erro remoto"));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.ListFailed");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnMappedMerchants()
    {
        var query = new GetIfoodMerchantsListQuery(1, 2, 50);
        _tokenProvider.GetAccessTokenAsync(1, Arg.Any<CancellationToken>()).Returns("token-1");
        _merchantClient.ListMerchantsAsync("token-1", 2, 50, Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantListResult(true, [new IfoodMerchantSummaryDto("m1", "Loja 1", "Empresa LTDA")], null));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(m => m.Id == "m1" && m.Name == "Loja 1");
    }
}
