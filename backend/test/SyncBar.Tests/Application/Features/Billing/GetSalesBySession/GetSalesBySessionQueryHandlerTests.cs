using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Billing.GetSalesBySession;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Billing.GetSalesBySession;

public sealed class GetSalesBySessionQueryHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetSalesBySessionQueryHandler _handler;

    public GetSalesBySessionQueryHandlerTests()
    {
        _handler = new GetSalesBySessionQueryHandler(_saleRepository, _logRepository, _unitOfWork);
    }

    private static Sale CreateSale(
        long cashSessionId,
        long saleNumber = 1001,
        long customerOrderId = 100,
        decimal subtotalAmount = 50m,
        decimal discountAmount = 0m,
        decimal serviceFeeAmount = 0m)
        => Sale.Create(
            branchId: 1,
            customerOrderId: customerOrderId,
            cashSessionId: cashSessionId,
            employeeId: 5,
            saleNumber: saleNumber,
            subtotalAmount: subtotalAmount,
            discountAmount: discountAmount,
            serviceFeeAmount: serviceFeeAmount).Value;

    [Fact]
    public async Task Handle_NoSalesForSession_ShouldReturnEmptyCollection()
    {
        var query = new GetSalesBySessionQuery(CashSessionId: 10);
        _saleRepository.GetByCashSessionAsync(query.CashSessionId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Sale>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SaleWithActiveAndInactivePayments_ShouldMapFieldsAndExcludeInactivePayments()
    {
        var query = new GetSalesBySessionQuery(CashSessionId: 10);
        var sale = CreateSale(query.CashSessionId, subtotalAmount: 60m, discountAmount: 5m, serviceFeeAmount: 3m);

        sale.AddPayment(paymentMethodId: 1, amount: 30m, changeAmount: null, authorizationCode: null, allowsChange: false)
            .IsSuccess.Should().BeTrue();
        sale.AddPayment(paymentMethodId: 2, amount: 15m, changeAmount: null, authorizationCode: null, allowsChange: false)
            .IsSuccess.Should().BeTrue();
        sale.Payments.Single(p => p.PaymentMethodId == 2).Deactivate();

        _saleRepository.GetByCashSessionAsync(query.CashSessionId, Arg.Any<CancellationToken>())
            .Returns([sale]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var response = result.Value.Single();
        response.Id.Should().Be(sale.Id);
        response.SaleNumber.Should().Be(sale.SaleNumber);
        response.CustomerOrderId.Should().Be(sale.CustomerOrderId);
        response.TotalAmount.Should().Be(sale.TotalAmount);
        response.SoldAt.Should().Be(sale.SoldAt);

        // Só o pagamento ativo (método 1) deve aparecer no resumo; o método 2 foi desativado.
        response.PaymentSummary.Should().ContainSingle()
            .Which.Should().Be($"1:{30m:0.00}");
    }

    [Fact]
    public async Task Handle_MultipleSales_ShouldOrderBySoldAtDescending()
    {
        var query = new GetSalesBySessionQuery(CashSessionId: 10);
        var olderSale = CreateSale(query.CashSessionId, saleNumber: 1001);
        // Garante um SoldAt distinguível do primeiro, já que Sale.Create usa DateTime.Now internamente
        // e a resolução do relógio do Windows pode não diferenciar chamadas muito próximas.
        await Task.Delay(20);
        var newerSale = CreateSale(query.CashSessionId, saleNumber: 1002);

        _saleRepository.GetByCashSessionAsync(query.CashSessionId, Arg.Any<CancellationToken>())
            .Returns([olderSale, newerSale]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.ElementAt(0).SaleNumber.Should().Be(newerSale.SaleNumber);
        result.Value.ElementAt(1).SaleNumber.Should().Be(olderSale.SaleNumber);
    }
}
