using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Finance.GetSummary;

public sealed record GetBillingSummaryQuery(
    long BranchId,
    int ReferenceYear,
    int ReferenceMonth) : IQuery<BillingSummaryResponse>;
