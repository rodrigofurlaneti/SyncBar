using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.GetTableReadingValidationSetting;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.GetTableReadingValidationSetting;

public sealed class GetTableReadingValidationSettingQueryHandlerTests
{
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetTableReadingValidationSettingQueryHandler _handler;

    public GetTableReadingValidationSettingQueryHandlerTests()
    {
        _handler = new GetTableReadingValidationSettingQueryHandler(_diningTableRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoTablesInBranch_ShouldReturnAllFlagsDisabledByDefault()
    {
        var query = new GetTableReadingValidationSettingQuery(BranchId: 1);
        _diningTableRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DiningTable>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsCameraInputEnabled.Should().BeFalse();
        result.Value.IsBarcodeEnabled.Should().BeFalse();
        result.Value.IsQrCodeEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TablesConfigured_ShouldReturnFlagsFromFirstTable()
    {
        var query = new GetTableReadingValidationSettingQuery(BranchId: 1);
        var table = DiningTable.Create(query.BranchId, 1, 5, 4).Value;
        table.SetReadingValidationSettings(true, false, true);
        _diningTableRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(new[] { table });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsCameraInputEnabled.Should().BeTrue();
        result.Value.IsBarcodeEnabled.Should().BeFalse();
        result.Value.IsQrCodeEnabled.Should().BeTrue();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
