using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Merchant;

public sealed class IFoodMerchantResolutionTests
{
    private const long BranchId = 1;
    private const long CompanyId = 1;

    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();

    private static Branch CreateBranch(long companyId = CompanyId)
        => Branch.Create(companyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;

    private static IfoodIntegrationSetting CreateSetting(bool enabled = true, string? clientId = "client-1", string? ifoodCustomerId = null)
    {
        var setting = IfoodIntegrationSetting.Create(CompanyId).Value;
        setting.SaveCredentials(clientId, clientId is null ? null : "secret-encrypted", enabled, ifoodCustomerId);
        return setting;
    }

    private static IfoodMerchantMapping CreateMapping(long branchId = BranchId, string? merchantId = "merchant-1")
    {
        var mapping = IfoodMerchantMapping.Create(branchId).Value;
        mapping.SetMerchant(merchantId, merchantUuid: null);
        return mapping;
    }

    private Task<SyncBar.Domain.Primitives.Result<(long CompanyId, string MerchantId, string Token, string? IfoodCustomerId)>> Resolve()
        => IfoodMerchantResolution.ResolveAsync(BranchId, _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, CancellationToken.None);

    [Fact]
    public async Task ResolveAsync_BranchNotFound_ShouldReturnFailure()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
    }

    [Fact]
    public async Task ResolveAsync_SettingNull_ShouldReturnNotConfigured()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((IfoodIntegrationSetting?)null);

        var result = await Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NotConfigured");
    }

    [Fact]
    public async Task ResolveAsync_SettingDisabled_ShouldReturnNotConfigured()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(CreateSetting(enabled: false));

        var result = await Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NotConfigured");
    }

    [Fact]
    public async Task ResolveAsync_SettingWithEmptyClientId_ShouldReturnNotConfigured()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(CreateSetting(clientId: " "));

        var result = await Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NotConfigured");
    }

    [Fact]
    public async Task ResolveAsync_MappingNull_ShouldReturnNoMerchantId()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(CreateSetting());
        _mappingRepository.GetByBranchAsync(BranchId, Arg.Any<CancellationToken>()).Returns((IfoodMerchantMapping?)null);

        var result = await Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NoMerchantId");
    }

    [Fact]
    public async Task ResolveAsync_MappingWithEmptyMerchantId_ShouldReturnNoMerchantId()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(CreateSetting());
        _mappingRepository.GetByBranchAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateMapping(merchantId: " "));

        var result = await Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NoMerchantId");
    }

    [Fact]
    public async Task ResolveAsync_TokenProviderReturnsNull_ShouldReturnNoToken()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(CreateSetting());
        _mappingRepository.GetByBranchAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateMapping());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NoToken");
    }

    [Fact]
    public async Task ResolveAsync_Success_ShouldReturnExpectedTuple()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(CreateSetting(ifoodCustomerId: "ifood-customer-1"));
        _mappingRepository.GetByBranchAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateMapping(merchantId: "merchant-42"));
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns("valid-token");

        var result = await Resolve();

        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(CompanyId);
        result.Value.MerchantId.Should().Be("merchant-42");
        result.Value.Token.Should().Be("valid-token");
        result.Value.IfoodCustomerId.Should().Be("ifood-customer-1");
    }

    [Fact]
    public async Task ResolveAsync_SuccessWithoutIfoodCustomerId_ShouldReturnNullInTuple()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(CreateSetting(ifoodCustomerId: null));
        _mappingRepository.GetByBranchAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateMapping());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns("valid-token");

        var result = await Resolve();

        result.IsSuccess.Should().BeTrue();
        result.Value.IfoodCustomerId.Should().BeNull();
    }
}
