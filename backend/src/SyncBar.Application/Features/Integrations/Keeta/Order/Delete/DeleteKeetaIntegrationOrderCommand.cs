using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.Delete
{
    public sealed record DeleteKeetaIntegrationOrderCommand(
        long Id,
        long CompanyId) : ICommand;
}
