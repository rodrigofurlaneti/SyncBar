using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;

public sealed class GetByAsaasPaymentIdQueryHandlerTests
{
    private readonly IAsaasIntegrationPaymentRepository _paymentRepository = Substitute.For<IAsaasIntegrationPaymentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetByAsaasPaymentIdQueryHandler _handler;

    public GetByAsaasPaymentIdQueryHandlerTests()
    {
        _handler = new GetByAsaasPaymentIdQueryHandler(_paymentRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ShouldReturnNotFound()
    {
        _paymentRepository.GetByAsaasPaymentIdAsync("pay_1", Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        var result = await _handler.Handle(new GetByAsaasPaymentIdQuery("pay_1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasPayment.NotFound");
    }

    [Fact]
    public async Task Handle_PaymentFound_ShouldReturnMappedResponse()
    {
        var payment = AsaasIntegrationPayment.Create(1, 10, 1, "pay_1", "PIX", 100m, new DateTime(2026, 9, 20)).Value;
        _paymentRepository.GetByAsaasPaymentIdAsync("pay_1", Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(new GetByAsaasPaymentIdQuery("pay_1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerOrderId.Should().Be(10);
    }
}
