using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByCustomerOrderId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.GetByCustomerOrderId;

public sealed class GetByCustomerOrderIdQueryHandlerTests
{
    private readonly IAsaasIntegrationPaymentRepository _paymentRepository = Substitute.For<IAsaasIntegrationPaymentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetByCustomerOrderIdQueryHandler _handler;

    public GetByCustomerOrderIdQueryHandlerTests()
    {
        _handler = new GetByCustomerOrderIdQueryHandler(_paymentRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoPaymentForOrder_ShouldReturnNotFound()
    {
        _paymentRepository.GetByCustomerOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        var result = await _handler.Handle(new GetByCustomerOrderIdQuery(10), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasPayment.NotFound");
    }

    [Fact]
    public async Task Handle_PaymentFound_ShouldReturnMappedResponse()
    {
        var payment = AsaasIntegrationPayment.Create(1, 10, 1, "pay_1", "PIX", 100m, new DateTime(2026, 9, 20)).Value;
        _paymentRepository.GetByCustomerOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(new GetByCustomerOrderIdQuery(10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AsaasPaymentId.Should().Be("pay_1");
    }
}
