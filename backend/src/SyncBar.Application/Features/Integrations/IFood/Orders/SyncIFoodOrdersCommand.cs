using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

// Um ciclo de polling para UMA empresa — disparado pelo IFoodOrderPollingBackgroundService a
// cada 30s para cada empresa com integração habilitada. Não é chamado pela API/frontend.
public sealed record SyncIFoodOrdersCommand(long CompanyId) : ICommand;
