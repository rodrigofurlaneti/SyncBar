using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Merchant;

public sealed class SaveIfoodOpeningHoursCommandHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodMerchantClient _merchantClient = Substitute.For<IIfoodMerchantClient>();
    private readonly IIfoodOpeningHoursRepository _openingHoursRepository = Substitute.For<IIfoodOpeningHoursRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SaveIfoodOpeningHoursCommandHandler _handler;

    public SaveIfoodOpeningHoursCommandHandlerTests()
    {
        _handler = new SaveIfoodOpeningHoursCommandHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _merchantClient,
            _openingHoursRepository, _logRepository, _unitOfWork);
    }

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    private void SetupResolvedMerchant(Branch branch, string merchantId = "MERCH-1", string token = "token-1")
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials("client-1", "encrypted", enabled: true, ifoodCustomerId: null);
        var mapping = IfoodMerchantMapping.Create(branchId: 1).Value;
        mapping.SetMerchant(merchantId, "uuid-1");

        _branchRepository.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(setting);
        _mappingRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(mapping);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(token);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ShouldPropagateResolutionFailure()
    {
        var command = new SaveIfoodOpeningHoursCommand(1, [new IfoodOpeningHourShiftInput(1, "09:00", 480)]);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
    }

    [Fact]
    public async Task Handle_InvalidStartTime_ShouldReturnInvalidStart()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new SaveIfoodOpeningHoursCommand(1, [new IfoodOpeningHourShiftInput(1, "not-a-time", 480)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodOpeningHours.InvalidStart");
    }

    [Fact]
    public async Task Handle_IfoodSyncFails_ShouldReturnSyncFailedAndNotPersistLocally()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new SaveIfoodOpeningHoursCommand(1, [new IfoodOpeningHourShiftInput(1, "09:00", 480)]);
        _merchantClient.SetOpeningHoursAsync("token-1", "MERCH-1", Arg.Any<IReadOnlyCollection<IfoodOpeningHourShift>>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantActionResult(false, "erro remoto"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.SyncOpeningHoursFailed");
        await _openingHoursRepository.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<IfoodOpeningHours>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeactivateExistingShiftsAndPersistNewOnes()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new SaveIfoodOpeningHoursCommand(1, [new IfoodOpeningHourShiftInput(1, "09:00", 480)]);
        _merchantClient.SetOpeningHoursAsync("token-1", "MERCH-1", Arg.Any<IReadOnlyCollection<IfoodOpeningHourShift>>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantActionResult(true, null));

        var existingShift = IfoodOpeningHours.Create(1, 2, new TimeSpan(8, 0, 0), 600).Value;
        _openingHoursRepository.GetByBranchForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns([existingShift]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existingShift.IsActive.Should().BeFalse();
        await _openingHoursRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<IfoodOpeningHours>>(l => l.Count() == 1), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
