using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Shipping;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Shipping;

public sealed class GetIfoodShippingQuoteQueryHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodShippingClient _shippingClient = Substitute.For<IIfoodShippingClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodShippingQuoteQueryHandler _handler;

    public GetIfoodShippingQuoteQueryHandlerTests()
    {
        _handler = new GetIfoodShippingQuoteQueryHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _shippingClient, _logRepository, _unitOfWork);
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
        var query = new GetIfoodShippingQuoteQuery(1, -23.5, -46.6);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
    }

    [Fact]
    public async Task Handle_IfoodApiFails_ShouldReturnQuoteFailed()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodShippingQuoteQuery(1, -23.5, -46.6);
        _shippingClient.GetDeliveryAvailabilitiesAsync("token-1", "MERCH-1", -23.5, -46.6, Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingQuoteResult(false, "erro remoto", null, 0, 0, 0, 0, 0, 0, null));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodShipping.QuoteFailed");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnMappedQuote()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodShippingQuoteQuery(1, -23.5, -46.6);
        _shippingClient.GetDeliveryAvailabilitiesAsync("token-1", "MERCH-1", -23.5, -46.6, Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingQuoteResult(true, null, "quote-1", 10m, 1m, 9m, 20, 40, 1500, DateTime.UtcNow.AddMinutes(10)));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.QuoteId.Should().Be("quote-1");
    }
}
