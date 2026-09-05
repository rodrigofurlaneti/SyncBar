using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Payment.ExistsByAsaasPaymentId;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.ExistsByAsaasPaymentId;

public sealed class ExistsByAsaasPaymentIdQueryHandlerTests
{
    private readonly IAsaasIntegrationPaymentRepository _paymentRepository = Substitute.For<IAsaasIntegrationPaymentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ExistsByAsaasPaymentIdQueryHandler _handler;

    public ExistsByAsaasPaymentIdQueryHandlerTests()
    {
        _handler = new ExistsByAsaasPaymentIdQueryHandler(_paymentRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_PaymentExists_ShouldReturnTrue()
    {
        _paymentRepository.ExistsByAsaasPaymentIdAsync("pay_1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new ExistsByAsaasPaymentIdQuery("pay_1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PaymentDoesNotExist_ShouldReturnFalse()
    {
        _paymentRepository.ExistsByAsaasPaymentIdAsync("pay_1", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new ExistsByAsaasPaymentIdQuery("pay_1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
