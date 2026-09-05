using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Delete;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.Delete;

public sealed class DeleteAsaasPaymentCommandHandlerTests
{
    private readonly IAsaasIntegrationPaymentRepository _paymentRepository = Substitute.For<IAsaasIntegrationPaymentRepository>();
    private readonly IAsaasService _asaasService = Substitute.For<IAsaasService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeleteAsaasPaymentCommandHandler _handler;

    public DeleteAsaasPaymentCommandHandlerTests()
    {
        _handler = new DeleteAsaasPaymentCommandHandler(_paymentRepository, _asaasService, _logRepository, _unitOfWork);
    }

    private static AsaasIntegrationPayment CreatePayment() =>
        AsaasIntegrationPayment.Create(1, 10, 1, "pay_1", "PIX", 100m, new DateTime(2026, 9, 20)).Value;

    [Fact]
    public async Task Handle_PaymentNotFound_ShouldReturnNotFound()
    {
        _paymentRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        var result = await _handler.Handle(new DeleteAsaasPaymentCommand(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasPayment.NotFound");
    }

    [Theory]
    [InlineData("RECEIVED")]
    [InlineData("CONFIRMED")]
    public async Task Handle_PaymentAlreadySettled_ShouldReturnConflict(string status)
    {
        var payment = CreatePayment();
        payment.UpdateStatus(status);
        _paymentRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(new DeleteAsaasPaymentCommand(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasPayment.CannotDeletePaid");
        await _asaasService.DidNotReceive().DeletePaymentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AsaasApiThrows_ShouldReturnFailureWithoutDeletingLocally()
    {
        var payment = CreatePayment();
        _paymentRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(payment);
        _asaasService.DeletePaymentAsync("pay_1", Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new HttpRequestException("Asaas indisponível"));

        var result = await _handler.Handle(new DeleteAsaasPaymentCommand(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasApi.DeleteFailed");
        _paymentRepository.DidNotReceive().Delete(Arg.Any<AsaasIntegrationPayment>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCancelRemotelyAndDeleteLocally()
    {
        var payment = CreatePayment();
        _paymentRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(new DeleteAsaasPaymentCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _asaasService.Received(1).DeletePaymentAsync("pay_1", Arg.Any<CancellationToken>());
        _paymentRepository.Received(1).Delete(payment);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
