using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Shipping;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Shipping;

public sealed class GetIfoodShippingCancellationReasonsQueryHandlerTests
{
    private readonly IIfoodShippingDeliveryRepository _deliveryRepository = Substitute.For<IIfoodShippingDeliveryRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodShippingClient _shippingClient = Substitute.For<IIfoodShippingClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodShippingCancellationReasonsQueryHandler _handler;

    public GetIfoodShippingCancellationReasonsQueryHandlerTests()
    {
        _handler = new GetIfoodShippingCancellationReasonsQueryHandler(
            _deliveryRepository, _branchRepository, _tokenProvider, _shippingClient, _logRepository, _unitOfWork);
    }

    private static IfoodShippingDelivery CreateDelivery() =>
        IfoodShippingDelivery.Create(
            1, "order-ref-1", "Cliente Teste", "11", "999998888", "01310000", "Av. Paulista", "1000", null,
            "Bela Vista", "São Paulo", "SP", "BR", null, null, null, 15m, "quote-1", "delivery-1", null, DateTime.UtcNow).Value;

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    [Fact]
    public async Task Handle_DeliveryNotFound_ShouldReturnNotFound()
    {
        var query = new GetIfoodShippingCancellationReasonsQuery(1);
        _deliveryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((IfoodShippingDelivery?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodShippingDelivery.NotFound");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnMappedReasons()
    {
        var delivery = CreateDelivery();
        var branch = CreateBranch();
        var query = new GetIfoodShippingCancellationReasonsQuery(1);
        _deliveryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(delivery.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _shippingClient.GetCancellationReasonsAsync("token-1", "delivery-1", Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<IfoodShippingCancellationReasonDto>)[new IfoodShippingCancellationReasonDto("code-1", "Cliente desistiu")]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(r => r.CancelCodeId == "code-1" && r.Description == "Cliente desistiu");
    }

    [Fact]
    public async Task Handle_NoReasonsReturned_ShouldReturnEmptyList()
    {
        var delivery = CreateDelivery();
        var branch = CreateBranch();
        var query = new GetIfoodShippingCancellationReasonsQuery(1);
        _deliveryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(delivery.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _shippingClient.GetCancellationReasonsAsync("token-1", "delivery-1", Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<IfoodShippingCancellationReasonDto>)[]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
