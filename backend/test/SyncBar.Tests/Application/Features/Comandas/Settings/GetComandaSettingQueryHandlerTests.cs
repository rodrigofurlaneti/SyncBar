using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Comandas.Settings;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Comandas.Settings;

public sealed class GetComandaSettingQueryHandlerTests
{
    private readonly IComandaSettingRepository _settingRepository = Substitute.For<IComandaSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetComandaSettingQueryHandler _handler;

    public GetComandaSettingQueryHandlerTests()
    {
        _handler = new GetComandaSettingQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoSettingConfigured_ShouldReturnZeroAsDefaultLimit()
    {
        var query = new GetComandaSettingQuery(BranchId: 1);
        _settingRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns((ComandaSetting?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DefaultLimitAmount.Should().Be(0);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SettingConfigured_ShouldReturnItsDefaultLimit()
    {
        var query = new GetComandaSettingQuery(BranchId: 1);
        var setting = ComandaSetting.Create(query.BranchId, 150m).Value;
        _settingRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(setting);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DefaultLimitAmount.Should().Be(150m);
    }
}
