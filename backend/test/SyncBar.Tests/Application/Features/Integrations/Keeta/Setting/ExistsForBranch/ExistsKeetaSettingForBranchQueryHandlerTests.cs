using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Setting.ExistsForBranch;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.ExistsForBranch;

public sealed class ExistsKeetaSettingForBranchQueryHandlerTests
{
    private readonly IKeetaIntegrationSettingRepository _settingRepository = Substitute.For<IKeetaIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ExistsKeetaSettingForBranchQueryHandler _handler;

    public ExistsKeetaSettingForBranchQueryHandlerTests()
    {
        _handler = new ExistsKeetaSettingForBranchQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingExists_ShouldReturnTrue()
    {
        _settingRepository.ExistsForBranchAsync(2, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new ExistsKeetaSettingForBranchQuery(2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SettingDoesNotExist_ShouldReturnFalse()
    {
        _settingRepository.ExistsForBranchAsync(2, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new ExistsKeetaSettingForBranchQuery(2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
