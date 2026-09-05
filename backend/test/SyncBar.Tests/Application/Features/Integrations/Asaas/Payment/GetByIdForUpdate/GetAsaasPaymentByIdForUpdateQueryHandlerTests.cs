using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByIdForUpdate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.GetByIdForUpdate;

public sealed class GetAsaasPaymentByIdForUpdateQueryHandlerTests
{
    private readonly IAsaasIntegrationPaymentRepository _paymentRepository = Substitute.For<IAsaasIntegrationPaymentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAsaasPaymentByIdForUpdateQueryHandler _handler;

    public GetAsaasPaymentByIdForUpdateQueryHandlerTests()
    {
        _handler = new GetAsaasPaymentByIdForUpdateQueryHandler(_paymentRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ShouldReturnNotFound()
    {
        _paymentRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        var result = await _handler.Handle(new GetAsaasPaymentByIdForUpdateQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasPayment.NotFound");
    }

    [Fact]
    public async Task Handle_PaymentFound_ShouldReturnMappedResponse()
    {
        var payment = AsaasIntegrationPayment.Create(1, 10, 1, "pay_1", "PIX", 100m, new DateTime(2026, 9, 20)).Value;
        _paymentRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(new GetAsaasPaymentByIdForUpdateQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AsaasPaymentId.Should().Be("pay_1");
    }
}
