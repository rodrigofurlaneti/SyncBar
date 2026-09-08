using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.BranchPaymentMethodSetting.ExistsForCompany;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.BranchPaymentMethodSetting.ExistsForCompany;

public sealed class ExistsBranchPaymentMethodSettingForCompanyQueryHandlerTests
{
    private readonly IBranchPaymentMethodSettingRepository _settingRepository = Substitute.For<IBranchPaymentMethodSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ExistsBranchPaymentMethodSettingForCompanyQueryHandler _handler;

    public ExistsBranchPaymentMethodSettingForCompanyQueryHandlerTests()
    {
        _handler = new ExistsBranchPaymentMethodSettingForCompanyQueryHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_SettingExistsForCompany_ShouldReturnTrue()
    {
        _settingRepository.ExistsForCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new ExistsBranchPaymentMethodSettingForCompanyQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoSettingForCompany_ShouldReturnFalse()
    {
        _settingRepository.ExistsForCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new ExistsBranchPaymentMethodSettingForCompanyQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
