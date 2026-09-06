using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.Update
{
    public sealed record UpdateKeetaIntegrationOrderCommand(
        long Id,
        long CompanyId,
        string? Status = null) : ICommand;
}
