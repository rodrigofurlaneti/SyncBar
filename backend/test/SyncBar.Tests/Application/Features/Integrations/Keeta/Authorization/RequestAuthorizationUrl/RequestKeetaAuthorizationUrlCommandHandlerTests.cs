using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.RequestAuthorizationUrl;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Authorization.RequestAuthorizationUrl;

public sealed class RequestKeetaAuthorizationUrlCommandHandlerTests
{
    private readonly IKeetaAuthClient _authClient = Substitute.For<IKeetaAuthClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RequestKeetaAuthorizationUrlCommandHandler _handler;

    public RequestKeetaAuthorizationUrlCommandHandlerTests()
    {
        _handler = new RequestKeetaAuthorizationUrlCommandHandler(_authClient, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_RedirectUriWithoutQueryString_ShouldAppendCorrelationWithQuestionMark()
    {
        var command = new RequestKeetaAuthorizationUrlCommand(1, 2, "https://app.syncbar.com/api/keeta/oauth/callback");
        _authClient.GetAuthorizationUrlAsync(1, 2, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://merchant.mykeeta.com/authorize?x=1");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MerchantAuthorizationUrl.Should().Be("https://merchant.mykeeta.com/authorize?x=1");
        await _authClient.Received(1).GetAuthorizationUrlAsync(
            1, 2, "https://app.syncbar.com/api/keeta/oauth/callback?companyId=1&branchId=2", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RedirectUriWithExistingQueryString_ShouldAppendCorrelationWithAmpersand()
    {
        var command = new RequestKeetaAuthorizationUrlCommand(1, 2, "https://app.syncbar.com/callback?foo=bar");
        _authClient.GetAuthorizationUrlAsync(1, 2, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://merchant.mykeeta.com/authorize");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _authClient.Received(1).GetAuthorizationUrlAsync(
            1, 2, "https://app.syncbar.com/callback?foo=bar&companyId=1&branchId=2", Arg.Any<CancellationToken>());
    }
}
