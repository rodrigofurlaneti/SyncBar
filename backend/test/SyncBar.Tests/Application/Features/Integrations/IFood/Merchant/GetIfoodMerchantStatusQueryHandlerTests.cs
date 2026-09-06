using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Merchant;

public sealed class GetIfoodMerchantStatusQueryHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodMerchantClient _merchantClient = Substitute.For<IIfoodMerchantClient>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodMerchantStatusQueryHandler _handler;

    public GetIfoodMerchantStatusQueryHandlerTests()
    {
        _handler = new GetIfoodMerchantStatusQueryHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _merchantClient, _cache, _logRepository, _unitOfWork);
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
        var query = new GetIfoodMerchantStatusQuery(1);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
    }

    [Fact]
    public async Task Handle_IfoodApiFails_ShouldReturnStatusFailed()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodMerchantStatusQuery(1);
        _merchantClient.GetStatusAsync("token-1", "MERCH-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantStatusResult(false, null, false, [], "erro remoto"));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.StatusFailed");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnMappedStatusAndCacheResult()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodMerchantStatusQuery(1);
        _merchantClient.GetStatusAsync("token-1", "MERCH-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantStatusResult(true, "OK", true, [], null));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Available.Should().BeTrue();

        // Segunda chamada deve vir do cache — sem nova chamada ao client remoto.
        var second = await _handler.Handle(query, CancellationToken.None);
        second.IsSuccess.Should().BeTrue();
        await _merchantClient.Received(1).GetStatusAsync("token-1", "MERCH-1", Arg.Any<CancellationToken>());
    }
}
