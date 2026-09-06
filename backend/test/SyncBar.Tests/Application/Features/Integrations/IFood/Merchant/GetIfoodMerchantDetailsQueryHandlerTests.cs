using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Merchant;

public sealed class GetIfoodMerchantDetailsQueryHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodMerchantClient _merchantClient = Substitute.For<IIfoodMerchantClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodMerchantDetailsQueryHandler _handler;

    public GetIfoodMerchantDetailsQueryHandlerTests()
    {
        _handler = new GetIfoodMerchantDetailsQueryHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _merchantClient, _logRepository, _unitOfWork);
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
        var query = new GetIfoodMerchantDetailsQuery(1);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
    }

    [Fact]
    public async Task Handle_IfoodApiFails_ShouldReturnDetailsFailed()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodMerchantDetailsQuery(1);
        _merchantClient.GetMerchantDetailsAsync("token-1", "MERCH-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantDetailsResult(false, null, null, null, null, null, null, null, null, "erro remoto"));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.DetailsFailed");
    }

    [Fact]
    public async Task Handle_WithAddress_ShouldMapAddressFields()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodMerchantDetailsQuery(1);
        var address = new IfoodMerchantAddressDto("BR", "SP", "São Paulo", "01000-000", "Centro", "Rua X", "100", -23.5, -46.6);
        _merchantClient.GetMerchantDetailsAsync("token-1", "MERCH-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantDetailsResult(true, "MERCH-1", "Loja Centro", "Empresa LTDA", "desc", "RESTAURANT", "AVAILABLE", DateTime.UtcNow, address, null));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().NotBeNull();
        result.Value.Address!.City.Should().Be("São Paulo");
        result.Value.Name.Should().Be("Loja Centro");
    }

    [Fact]
    public async Task Handle_WithoutAddress_ShouldReturnNullAddress()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodMerchantDetailsQuery(1);
        _merchantClient.GetMerchantDetailsAsync("token-1", "MERCH-1", Arg.Any<CancellationToken>())
            .Returns(new IfoodMerchantDetailsResult(true, "MERCH-1", "Loja Centro", null, null, null, null, null, null, null));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().BeNull();
    }
}
