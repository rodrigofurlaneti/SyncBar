using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Finance.GetCommissionReport;

public sealed record EmployeeCommissionResponse(
    long EmployeeId,
    string EmployeeName,
    decimal? CommissionPercent,
    int SalesCount,
    decimal Revenue,
    decimal CommissionAmount);

public sealed record GetCommissionReportQuery(long BranchId, DateTime From, DateTime To)
    : IQuery<IReadOnlyCollection<EmployeeCommissionResponse>>;
