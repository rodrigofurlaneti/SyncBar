using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Shipping;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Shipping;

public sealed class RequestIfoodShippingDriverCommandHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodShippingClient _shippingClient = Substitute.For<IIfoodShippingClient>();
    private readonly IIfoodShippingDeliveryRepository _deliveryRepository = Substitute.For<IIfoodShippingDeliveryRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RequestIfoodShippingDriverCommandHandler _handler;
    private static readonly DateTime Now = new(2026, 9, 3, 10, 0, 0);

    public RequestIfoodShippingDriverCommandHandlerTests()
    {
        _handler = new RequestIfoodShippingDriverCommandHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _shippingClient,
            _deliveryRepository, _timeProvider, _logRepository, _unitOfWork);

        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(Now, TimeSpan.Zero));
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);
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
        _shippingClient.GetDeliveryAvailabilitiesAsync(token, merchantId, Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingQuoteResult(true, null, "quote-1", 15, 0, 15, 10, 20, 1000, Now.AddHours(1)));
    }

    [Fact]
    public async Task Handle_UnavailableCoverage_ShouldNotCreateExternalOrder()
    {
        SetupResolvedMerchant(CreateBranch());
        _shippingClient.GetDeliveryAvailabilitiesAsync("token-1", "MERCH-1", Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingQuoteResult(false, "Sem cobertura", null, 0, 0, 0, 0, 0, 0, null));
        var result = await _handler.Handle(ValidCommand(), default);
        result.Error.Code.Should().Be("IfoodShipping.Unavailable");
        await _shippingClient.DidNotReceiveWithAnyArgs().RequestDriverAsync(default!, default!, default!, default);
        await _deliveryRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    private static RequestIfoodShippingDriverCommand ValidCommand() =>
        new(1, "order-ref-1", "Cliente Teste", "11", "999998888", 15m, "quote-1",
            "01310000", "1000", "Av. Paulista", null, "Bela Vista", "São Paulo", "SP", null, null, -23.5, -46.6,
            [new IfoodShippingItemInput("Produto 1", null, 2, 10m)]);

    [Fact]
    public async Task Handle_BranchNotFound_ShouldPropagateResolutionFailure()
    {
        var command = ValidCommand();
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
    }

    [Fact]
    public async Task Handle_IfoodApiFails_ShouldReturnRequestDriverFailed()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = ValidCommand();
        _shippingClient.RequestDriverAsync("token-1", "MERCH-1", Arg.Any<IfoodShippingRequestDriverPayload>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingRequestDriverResult(false, "erro remoto", null, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodShipping.RequestDriverFailed");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistDeliveryAndReturnId()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = ValidCommand();
        _shippingClient.RequestDriverAsync("token-1", "MERCH-1", Arg.Any<IfoodShippingRequestDriverPayload>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingRequestDriverResult(true, null, "delivery-1", "https://track"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _deliveryRepository.Received(1).AddAsync(
            Arg.Is<IfoodShippingDelivery>(d => d.IfoodDeliveryId == "delivery-1" && d.TrackingUrl == "https://track"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
