using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Catalog.OptionGroups;

public sealed class UpdateIfoodOptionGroupCommandHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodCatalogClient _catalogClient = Substitute.For<IIfoodCatalogClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateIfoodOptionGroupCommandHandler _handler;

    public UpdateIfoodOptionGroupCommandHandlerTests()
    {
        _handler = new UpdateIfoodOptionGroupCommandHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _catalogClient, _logRepository, _unitOfWork);
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
        var command = new UpdateIfoodOptionGroupCommand(1, Guid.NewGuid(), "Bebidas");
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
    }

    [Fact]
    public async Task Handle_IfoodApiFails_ShouldReturnUpdateOptionGroupFailed()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new UpdateIfoodOptionGroupCommand(1, Guid.NewGuid(), "Bebidas");
        _catalogClient.UpdateOptionGroupAsync("token-1", "MERCH-1", command.OptionGroupId, "Bebidas", Arg.Any<CancellationToken>())
            .Returns(new IfoodCatalogActionResult(false, "erro remoto"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodCatalog.UpdateOptionGroupFailed");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldSucceed()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new UpdateIfoodOptionGroupCommand(1, Guid.NewGuid(), "Bebidas");
        _catalogClient.UpdateOptionGroupAsync("token-1", "MERCH-1", command.OptionGroupId, "Bebidas", Arg.Any<CancellationToken>())
            .Returns(new IfoodCatalogActionResult(true, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
