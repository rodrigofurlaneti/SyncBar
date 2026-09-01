using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

// Um ciclo de polling para UMA empresa — disparado pelo IfoodOrderPollingBackgroundService a
// cada 30s para cada empresa com integração habilitada. Não é chamado pela API/frontend.
public sealed record SyncIfoodOrdersCommand(long CompanyId) : ICommand;
