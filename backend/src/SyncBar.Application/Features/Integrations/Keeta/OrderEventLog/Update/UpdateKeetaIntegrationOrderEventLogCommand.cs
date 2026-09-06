using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Update
{
    public sealed record UpdateKeetaIntegrationOrderEventLogCommand(
        long Id,
        long CompanyId,
        bool MarkAsProcessed = false,
        string? ErrorMessage = null) : ICommand;
}
