using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.Update;

public sealed class UpdateAsaasIntegrationPaymentCommandHandlerTests
{
    private readonly IAsaasIntegrationPaymentRepository _paymentRepository = Substitute.For<IAsaasIntegrationPaymentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateAsaasIntegrationPaymentCommandHandler _handler;

    public UpdateAsaasIntegrationPaymentCommandHandlerTests()
    {
        _handler = new UpdateAsaasIntegrationPaymentCommandHandler(_paymentRepository, _logRepository, _unitOfWork);
    }

    private static AsaasIntegrationPayment CreatePayment() =>
        AsaasIntegrationPayment.Create(1, 10, 1, "pay_1", "PIX", 100m, new DateTime(2026, 9, 20)).Value;

    [Fact]
    public async Task Handle_PaymentNotFound_ShouldReturnNotFound()
    {
        var command = new UpdateAsaasIntegrationPaymentCommand(1, "RECEIVED");
        _paymentRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasPayment.NotFound");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateStatusAndCommit()
    {
        var payment = CreatePayment();
        var command = new UpdateAsaasIntegrationPaymentCommand(1, "RECEIVED", NetValue: 98m, PaymentDate: new DateTime(2026, 9, 18));
        _paymentRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be("RECEIVED");
        payment.NetValue.Should().Be(98m);
        _paymentRepository.Received(1).Update(payment);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPixDetails_ShouldSetPixDetailsOnEntity()
    {
        var payment = CreatePayment();
        var command = new UpdateAsaasIntegrationPaymentCommand(1, "PENDING", PixQrCodeBase64: "base64", PixPayload: "copia-e-cola");
        _paymentRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.PixQrCodeBase64.Should().Be("base64");
        payment.PixPayload.Should().Be("copia-e-cola");
    }

    [Fact]
    public async Task Handle_WithUrls_ShouldSetUrlsOnEntity()
    {
        var payment = CreatePayment();
        var command = new UpdateAsaasIntegrationPaymentCommand(1, "PENDING", InvoiceUrl: "https://invoice", BankSlipUrl: "https://bankslip");
        _paymentRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.InvoiceUrl.Should().Be("https://invoice");
        payment.BankSlipUrl.Should().Be("https://bankslip");
    }
}
