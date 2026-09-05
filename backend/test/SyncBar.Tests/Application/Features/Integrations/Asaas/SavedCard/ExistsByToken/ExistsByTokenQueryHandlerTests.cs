using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.ExistsByToken;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.ExistsByToken;

public sealed class ExistsByTokenQueryHandlerTests
{
    private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository = Substitute.For<IAsaasIntegrationSavedCardRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ExistsByTokenQueryHandler _handler;

    public ExistsByTokenQueryHandlerTests()
    {
        _handler = new ExistsByTokenQueryHandler(_savedCardRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_TokenExists_ShouldReturnTrue()
    {
        _savedCardRepository.ExistsByTokenAsync("token-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new ExistsByTokenQuery("token-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TokenDoesNotExist_ShouldReturnFalse()
    {
        _savedCardRepository.ExistsByTokenAsync("token-1", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new ExistsByTokenQuery("token-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
