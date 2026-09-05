using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByBranchId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.GetByBranchId;

public sealed class GetByBranchIdQueryHandlerTests
{
    private readonly IAsaasIntegrationPaymentRepository _paymentRepository = Substitute.For<IAsaasIntegrationPaymentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetByBranchIdQueryHandler _handler;

    public GetByBranchIdQueryHandlerTests()
    {
        _handler = new GetByBranchIdQueryHandler(_paymentRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoPaymentsForBranch_ShouldReturnEmptyList()
    {
        _paymentRepository.GetByBranchIdAsync(1, Arg.Any<CancellationToken>()).Returns(new List<AsaasIntegrationPayment>());

        var result = await _handler.Handle(new GetByBranchIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_HasPayments_ShouldReturnMappedList()
    {
        var payments = new List<AsaasIntegrationPayment>
        {
            AsaasIntegrationPayment.Create(1, 10, 1, "pay_1", "PIX", 100m, new DateTime(2026, 9, 20)).Value,
            AsaasIntegrationPayment.Create(1, 11, 1, "pay_2", "BOLETO", 50m, new DateTime(2026, 9, 25)).Value,
        };
        _paymentRepository.GetByBranchIdAsync(1, Arg.Any<CancellationToken>()).Returns(payments);

        var result = await _handler.Handle(new GetByBranchIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
