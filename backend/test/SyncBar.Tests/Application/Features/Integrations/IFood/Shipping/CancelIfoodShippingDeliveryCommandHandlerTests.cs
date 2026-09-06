using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Shipping;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Shipping;

public sealed class CancelIfoodShippingDeliveryCommandHandlerTests
{
    private readonly IIfoodShippingDeliveryRepository _deliveryRepository = Substitute.For<IIfoodShippingDeliveryRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodShippingClient _shippingClient = Substitute.For<IIfoodShippingClient>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CancelIfoodShippingDeliveryCommandHandler _handler;
    private static readonly DateTime Now = new(2026, 9, 3, 10, 0, 0);

    public CancelIfoodShippingDeliveryCommandHandlerTests()
    {
        _handler = new CancelIfoodShippingDeliveryCommandHandler(
            _deliveryRepository, _branchRepository, _tokenProvider, _shippingClient, _timeProvider, _logRepository, _unitOfWork);

        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(Now, TimeSpan.Zero));
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);
    }

    private static IfoodShippingDelivery CreateDelivery() =>
        IfoodShippingDelivery.Create(
            1, "order-ref-1", "Cliente Teste", "11", "999998888", "01310000", "Av. Paulista", "1000", null,
            "Bela Vista", "São Paulo", "SP", "BR", null, null, null, 15m, "quote-1", "delivery-1", null, Now).Value;

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    [Fact]
    public async Task Handle_DeliveryNotFound_ShouldReturnNotFound()
    {
        var command = new CancelIfoodShippingDeliveryCommand(1, "cliente desistiu", 1);
        _deliveryRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((IfoodShippingDelivery?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodShippingDelivery.NotFound");
    }

    [Fact]
    public async Task Handle_BranchNotFound_ShouldReturnBranchNotFound()
    {
        var delivery = CreateDelivery();
        var command = new CancelIfoodShippingDeliveryCommand(1, "cliente desistiu", 1);
        _deliveryRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(delivery.BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task Handle_IfoodApiFails_ShouldReturnCancelFailed()
    {
        var delivery = CreateDelivery();
        var branch = CreateBranch();
        var command = new CancelIfoodShippingDeliveryCommand(1, "cliente desistiu", 1);
        _deliveryRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(delivery.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _shippingClient.CancelAsync("token-1", "delivery-1", "cliente desistiu", 1, Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingActionResult(false, "erro remoto"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodShipping.CancelFailed");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldMarkDeliveryCancelledAndCommit()
    {
        var delivery = CreateDelivery();
        var branch = CreateBranch();
        var command = new CancelIfoodShippingDeliveryCommand(1, "cliente desistiu", 1);
        _deliveryRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(delivery.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _shippingClient.CancelAsync("token-1", "delivery-1", "cliente desistiu", 1, Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingActionResult(true, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        delivery.CancelledAt.Should().Be(Now);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_ShouldReturnFailure()
    {
        var delivery = CreateDelivery();
        var branch = CreateBranch();
        delivery.MarkCancelled("motivo anterior", Now);
        var command = new CancelIfoodShippingDeliveryCommand(1, "cliente desistiu", 1);
        _deliveryRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(delivery.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _shippingClient.CancelAsync("token-1", "delivery-1", "cliente desistiu", 1, Arg.Any<CancellationToken>())
            .Returns(new IfoodShippingActionResult(true, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodShippingDelivery.AlreadyCancelled");
    }
}
