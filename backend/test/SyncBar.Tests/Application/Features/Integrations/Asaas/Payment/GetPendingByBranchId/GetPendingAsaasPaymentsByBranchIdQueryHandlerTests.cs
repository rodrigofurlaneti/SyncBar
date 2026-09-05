using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetPendingByBranchId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.GetPendingByBranchId;

public sealed class GetPendingAsaasPaymentsByBranchIdQueryHandlerTests
{
    private readonly IAsaasIntegrationPaymentRepository _paymentRepository = Substitute.For<IAsaasIntegrationPaymentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetPendingAsaasPaymentsByBranchIdQueryHandler _handler;

    public GetPendingAsaasPaymentsByBranchIdQueryHandlerTests()
    {
        _handler = new GetPendingAsaasPaymentsByBranchIdQueryHandler(_paymentRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoPendingPayments_ShouldReturnEmptyList()
    {
        _paymentRepository.GetPendingByBranchIdAsync(1, Arg.Any<CancellationToken>()).Returns(new List<AsaasIntegrationPayment>());

        var result = await _handler.Handle(new GetPendingAsaasPaymentsByBranchIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_HasPendingPayments_ShouldReturnMappedList()
    {
        var payments = new List<AsaasIntegrationPayment>
        {
            AsaasIntegrationPayment.Create(1, 10, 1, "pay_1", "PIX", 100m, new DateTime(2026, 9, 20)).Value,
        };
        _paymentRepository.GetPendingByBranchIdAsync(1, Arg.Any<CancellationToken>()).Returns(payments);

        var result = await _handler.Handle(new GetPendingAsaasPaymentsByBranchIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(p => p.AsaasPaymentId == "pay_1");
    }
}
