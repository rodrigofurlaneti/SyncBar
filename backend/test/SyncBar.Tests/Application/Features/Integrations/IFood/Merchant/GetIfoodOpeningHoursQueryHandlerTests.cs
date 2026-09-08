using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Merchant;

public sealed class GetIfoodOpeningHoursQueryHandlerTests
{
    private readonly IIfoodOpeningHoursRepository _openingHoursRepository = Substitute.For<IIfoodOpeningHoursRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodOpeningHoursQueryHandler _handler;

    public GetIfoodOpeningHoursQueryHandlerTests()
    {
        _handler = new GetIfoodOpeningHoursQueryHandler(
            _openingHoursRepository, _mappingRepository, _settingRepository, _branchRepository, _logRepository, _unitOfWork,
            Substitute.For<IIfoodTokenProvider>(), Substitute.For<IIfoodMerchantClient>());
    }

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    [Fact]
    public async Task Handle_BranchNotFound_ShouldReturnHasCustomerIdFalse()
    {
        var query = new GetIfoodOpeningHoursQuery(1);
        _openingHoursRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>()).Returns([]);
        _mappingRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>()).Returns((IfoodMerchantMapping?)null);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasIfoodCustomerId.Should().BeFalse();
        result.Value.PreparationTimeMinutes.Should().BeNull();
    }

    [Fact]
    public async Task Handle_BranchWithCustomerIdConfigured_ShouldReturnHasCustomerIdTrue()
    {
        var branch = CreateBranch();
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials("client-1", "encrypted", enabled: true, ifoodCustomerId: "customer-1");

        var query = new GetIfoodOpeningHoursQuery(1);
        _openingHoursRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>()).Returns([]);
        _mappingRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>()).Returns((IfoodMerchantMapping?)null);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasIfoodCustomerId.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SettingWithoutCustomerId_ShouldReturnHasCustomerIdFalse()
    {
        var branch = CreateBranch();
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;

        var query = new GetIfoodOpeningHoursQuery(1);
        _openingHoursRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>()).Returns([]);
        _mappingRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>()).Returns((IfoodMerchantMapping?)null);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasIfoodCustomerId.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShiftsAndPreparationTime_ShouldMapCorrectly()
    {
        var mapping = IfoodMerchantMapping.Create(branchId: 1).Value;
        mapping.SetMerchant("MERCH-1", "uuid-1");
        mapping.SetPreparationTime(20);
        var shift = IfoodOpeningHours.Create(1, 1, new TimeSpan(9, 30, 0), 480).Value;

        var query = new GetIfoodOpeningHoursQuery(1);
        _openingHoursRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>()).Returns([shift]);
        _mappingRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>()).Returns(mapping);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PreparationTimeMinutes.Should().Be(20);
        result.Value.Shifts.Should().ContainSingle(s => s.DayOfWeek == 1 && s.Start == "09:30" && s.DurationMinutes == 480);
    }
}
