using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Merchant;

public sealed class SetIfoodPreparationTimeCommandHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodMerchantClient _merchantClient = Substitute.For<IIfoodMerchantClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SetIfoodPreparationTimeCommandHandler _handler;

    public SetIfoodPreparationTimeCommandHandlerTests()
    {
        _handler = new SetIfoodPreparationTimeCommandHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _merchantClient, _logRepository, _unitOfWork);
    }

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    private IfoodMerchantMapping SetupResolvedMerchant(Branch branch, string? ifoodCustomerId = "customer-1", string merchantId = "MERCH-1", string token = "token-1")
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials("client-1", "encrypted", enabled: true, ifoodCustomerId: ifoodCustomerId);
        var mapping = IfoodMerchantMapping.Create(branchId: 1).Value;
        mapping.SetMerchant(merchantId, "uuid-1");

        _branchRepository.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(setting);
        _mappingRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(mapping);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(token);
        return mapping;
    }

    [Fact]
    public async Task Handle_BranchNotFound_ShouldPropagateResolutionFailure()
    {
        var command = new SetIfoodPreparationTimeCommand(1, 20);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
    }

    [Fact]
    public async Task Handle_MissingIfoodCustomerId_ShouldReturnMissingCustomerId()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch, ifoodCustomerId: null);
        var command = new SetIfoodPreparationTimeCommand(1, 20);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.MissingCustomerId");
    }

    [Fact]
    public async Task Handle_MinutesNull_ShouldCallDeletePreparationTime()
    {
        var branch = CreateBranch();
        var mapping = SetupResolvedMerchant(branch);
        var command = new SetIfoodPreparationTimeCommand(1, null);
        _merchantClient.DeletePreparationTimeAsync("token-1", "MERCH-1", "customer-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantActionResult(true, null));
        _mappingRepository.GetByBranchForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(mapping);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        mapping.PreparationTimeMinutes.Should().BeNull();
        await _merchantClient.Received(1).DeletePreparationTimeAsync("token-1", "MERCH-1", "customer-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeletePreparationTimeFails_ShouldReturnFailure()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new SetIfoodPreparationTimeCommand(1, null);
        _merchantClient.DeletePreparationTimeAsync("token-1", "MERCH-1", "customer-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantActionResult(false, "erro remoto"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.DeletePreparationTimeFailed");
    }

    [Fact]
    public async Task Handle_MinutesProvided_ShouldCallUpsertPreparationTimeAndUpdateMapping()
    {
        var branch = CreateBranch();
        var mapping = SetupResolvedMerchant(branch);
        var command = new SetIfoodPreparationTimeCommand(1, 25);
        _merchantClient.UpsertPreparationTimeAsync("token-1", "MERCH-1", "customer-1", 25, Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantActionResult(true, null));
        _mappingRepository.GetByBranchForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(mapping);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        mapping.PreparationTimeMinutes.Should().Be(25);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpsertPreparationTimeFails_ShouldReturnFailure()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new SetIfoodPreparationTimeCommand(1, 25);
        _merchantClient.UpsertPreparationTimeAsync("token-1", "MERCH-1", "customer-1", 25, Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantActionResult(false, "erro remoto"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.SetPreparationTimeFailed");
    }

    [Fact]
    public async Task Handle_MappingNotFoundForUpdate_ShouldReturnNoMerchantId()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new SetIfoodPreparationTimeCommand(1, 25);
        _merchantClient.UpsertPreparationTimeAsync("token-1", "MERCH-1", "customer-1", 25, Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantActionResult(true, null));
        _mappingRepository.GetByBranchForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((IfoodMerchantMapping?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NoMerchantId");
    }
}
