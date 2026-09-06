namespace SyncBar.Application.Features.Integrations.Keeta.Order.Create
{
    public sealed record CreateKeetaIntegrationOrderResponse(
        long Id,
        long CompanyId,
        long BranchId,
        string KeetaOrderId,
        string DisplayId,
        string Status,
        decimal OrderAmount);
}
