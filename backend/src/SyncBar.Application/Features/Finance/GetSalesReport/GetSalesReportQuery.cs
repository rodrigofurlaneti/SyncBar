using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Finance.GetSalesReport;

public sealed record TopProductResponse(long ProductId, string ProductName, decimal Quantity, decimal Revenue);
public sealed record EmployeeSalesResponse(long EmployeeId, string EmployeeName, int SalesCount, decimal Revenue, decimal ServiceFee);
public sealed record WeekdayRevenueResponse(int DayOfWeek, decimal Revenue, int SalesCount);
public sealed record HourRevenueResponse(int Hour, decimal Revenue);
public sealed record CancelledItemResponse(string ProductName, decimal Quantity, string? CancelledBy, DateTime CancelledAt);

public sealed record SalesReportResponse(
    decimal Revenue,
    int SalesCount,
    decimal AverageTicket,
    decimal ServiceFeeTotal,
    IReadOnlyCollection<TopProductResponse> TopProducts,
    IReadOnlyCollection<EmployeeSalesResponse> SalesByEmployee,
    IReadOnlyCollection<WeekdayRevenueResponse> RevenueByWeekday,
    IReadOnlyCollection<HourRevenueResponse> RevenueByHour,
    int CancelledItemsCount,
    IReadOnlyCollection<CancelledItemResponse> CancelledItems);

public sealed record GetSalesReportQuery(long BranchId, int ReferenceYear, int ReferenceMonth) : IQuery<SalesReportResponse>;
