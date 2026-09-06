using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Delete
{
    public sealed record DeleteKeetaIntegrationOrderEventLogCommand(
        long Id,
        long CompanyId) : ICommand;
}
