using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Delete
{
    public sealed record DeleteKeetaIntegrationRefundDisputeCommand(
        long Id,
        long CompanyId) : ICommand;
}
