using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Finance.GetSalesReport;

internal sealed class GetSalesReportQueryHandler(
    ISaleRepository saleRepository,
    ICustomerOrderRepository orderRepository,
    IProductRepository productRepository,
    IEmployeeRepository employeeRepository)
    : IQueryHandler<GetSalesReportQuery, SalesReportResponse>
{
    public async Task<Result<SalesReportResponse>> Handle(GetSalesReportQuery request, CancellationToken cancellationToken)
    {
        if (request.ReferenceMonth is < 1 or > 12)
            return Result.Failure<SalesReportResponse>(
                new Error("SalesReport.InvalidMonth", "Reference month must be between 1 and 12."));

        var from = new DateTime(request.ReferenceYear, request.ReferenceMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);

        var sales = await saleRepository.GetByBranchAndPeriodAsync(request.BranchId, from, to, cancellationToken);
        var paidOrders = await orderRepository.GetByIdsAsync(
            sales.Select(s => s.CustomerOrderId).Distinct().ToList(), cancellationToken);
        var monthOrders = await orderRepository.GetByBranchAndPeriodAsync(request.BranchId, from, to, cancellationToken);
        var employees = await employeeRepository.GetByBranchAsync(request.BranchId, cancellationToken);

        var soldItems = paidOrders
            .SelectMany(o => o.Items)
            .Where(i => i.IsActive && i.OrderItemStatusId != OrderItemStatusIds.Cancelado)
            .ToList();
        var productIds = soldItems.Select(i => i.ProductId).Distinct().ToList();
        var products = await productRepository.GetByIdsAsync(productIds, cancellationToken);
        string ProductName(long id) => products.FirstOrDefault(p => p.Id == id)?.Name ?? $"Produto {id}";
        string? EmployeeName(long? id) => id is null ? null : employees.FirstOrDefault(e => e.Id == id)?.Name;

        var revenue = sales.Sum(s => s.TotalAmount);

        var topProducts = soldItems
            .GroupBy(i => i.ProductId)
            .Select(g => new TopProductResponse(g.Key, ProductName(g.Key), g.Sum(i => i.Quantity), g.Sum(i => i.TotalAmount)))
            .OrderByDescending(p => p.Revenue)
            .Take(10)
            .ToList();

        // Vendas atribuidas ao garcom que ABRIU o pedido (quem atendeu a mesa).
        var byEmployee = sales
            .Join(paidOrders, s => s.CustomerOrderId, o => o.Id, (s, o) => new { s, o })
            .GroupBy(x => x.o.EmployeeId)
            .Select(g => new EmployeeSalesResponse(
                g.Key, EmployeeName(g.Key) ?? $"Funcionario {g.Key}",
                g.Count(), g.Sum(x => x.s.TotalAmount), g.Sum(x => x.s.ServiceFeeAmount)))
            .OrderByDescending(e => e.Revenue)
            .ToList();

        // Horarios no fuso LOCAL do bar (SoldAt e gravado em UTC).
        var localSales = sales
            .Select(s => new { s, Local = DateTime.SpecifyKind(s.SoldAt, DateTimeKind.Utc).ToLocalTime() })
            .ToList();

        var byWeekday = Enumerable.Range(0, 7)
            .Select(d => new WeekdayRevenueResponse(
                d,
                localSales.Where(x => (int)x.Local.DayOfWeek == d).Sum(x => x.s.TotalAmount),
                localSales.Count(x => (int)x.Local.DayOfWeek == d)))
            .ToList();

        var byHour = localSales
            .GroupBy(x => x.Local.Hour)
            .OrderBy(g => g.Key)
            .Select(g => new HourRevenueResponse(g.Key, g.Sum(x => x.s.TotalAmount)))
            .ToList();

        var cancelled = monthOrders
            .SelectMany(o => o.Items)
            .Where(i => i.OrderItemStatusId == OrderItemStatusIds.Cancelado)
            .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
            .ToList();
        var cancelledProductIds = cancelled.Select(i => i.ProductId).Distinct().ToList();
        var cancelledProducts = await productRepository.GetByIdsAsync(cancelledProductIds, cancellationToken);

        var cancelledList = cancelled.Take(20)
            .Select(i => new CancelledItemResponse(
                cancelledProducts.FirstOrDefault(p => p.Id == i.ProductId)?.Name ?? $"Produto {i.ProductId}",
                i.Quantity,
                EmployeeName(i.CancelledByEmployeeId),
                i.UpdatedAt ?? i.CreatedAt))
            .ToList();

        return Result.Success(new SalesReportResponse(
            revenue,
            sales.Count,
            sales.Count == 0 ? 0 : Math.Round(revenue / sales.Count, 2),
            sales.Sum(s => s.ServiceFeeAmount),
            topProducts,
            byEmployee,
            byWeekday,
            byHour,
            cancelled.Count,
            cancelledList));
    }
}
