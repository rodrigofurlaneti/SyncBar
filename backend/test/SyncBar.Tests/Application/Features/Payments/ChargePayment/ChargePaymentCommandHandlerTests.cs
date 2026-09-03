using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.Application.Abstractions.Payments;
using SyncBar.Application.Features.Payments.ChargePayment;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Payments.ChargePayment;

public sealed class ChargePaymentCommandHandlerTests
{
    private readonly IPaymentGatewayService _gateway = Substitute.For<IPaymentGatewayService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ChargePaymentCommandHandler _handler;

    public ChargePaymentCommandHandlerTests()
    {
        _handler = new ChargePaymentCommandHandler(_gateway, _logRepository, _unitOfWork);
    }

    private static ChargePaymentCommand CreateValidCommand(
        long saleId = 1,
        decimal amount = 100m,
        PaymentGatewayMethod method = PaymentGatewayMethod.Pix,
        string? customerDocument = "12345678900")
        => new(saleId, amount, method, customerDocument);

    [Fact]
    public async Task Handle_GatewayApprovesCharge_ShouldReturnSuccessResponseWithGatewayData()
    {
        var command = CreateValidCommand(saleId: 10, amount: 55.50m, method: PaymentGatewayMethod.Pix, customerDocument: "12345678900");
        var chargeResult = new PaymentChargeResult("txn-123", PaymentChargeStatus.Approved, "00020126...qrcode", null);
        _gateway.ChargeAsync(Arg.Any<PaymentChargeRequest>(), Arg.Any<CancellationToken>()).Returns(chargeResult);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GatewayTransactionId.Should().Be("txn-123");
        result.Value.Status.Should().Be(nameof(PaymentChargeStatus.Approved));
        result.Value.QrCodePayload.Should().Be("00020126...qrcode");

        await _gateway.Received(1).ChargeAsync(
            Arg.Is<PaymentChargeRequest>(r =>
                r.SaleId == 10 &&
                r.Amount == 55.50m &&
                r.Method == PaymentGatewayMethod.Pix &&
                r.CustomerDocument == "12345678900"),
            Arg.Any<CancellationToken>());
        // Handler não chama CommitAsync explicitamente — só o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GatewayDeclinesChargeWithReason_ShouldReturnFailureWithGatewayReason()
    {
        var command = CreateValidCommand();
        var chargeResult = new PaymentChargeResult("txn-456", PaymentChargeStatus.Declined, null, "Saldo insuficiente");
        _gateway.ChargeAsync(Arg.Any<PaymentChargeRequest>(), Arg.Any<CancellationToken>()).Returns(chargeResult);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Payment.Declined");
        result.Error.Message.Should().Be("Saldo insuficiente");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GatewayDeclinesChargeWithoutReason_ShouldReturnFailureWithDefaultMessage()
    {
        var command = CreateValidCommand();
        var chargeResult = new PaymentChargeResult("txn-789", PaymentChargeStatus.Declined, null, null);
        _gateway.ChargeAsync(Arg.Any<PaymentChargeRequest>(), Arg.Any<CancellationToken>()).Returns(chargeResult);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Payment.Declined");
        result.Error.Message.Should().Be("Payment declined by gateway.");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GatewayThrowsException_ShouldPropagateExceptionButStillLogViaFinally()
    {
        var command = CreateValidCommand();
        _gateway.ChargeAsync(Arg.Any<PaymentChargeRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Gateway indisponível."));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Gateway indisponível.");
        // O finally da base grava o log e comita mesmo quando a action lança exceção.
        await _logRepository.Received(1).AddAsync(Arg.Any<SyncBar.Domain.Entities.LogTracker>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
