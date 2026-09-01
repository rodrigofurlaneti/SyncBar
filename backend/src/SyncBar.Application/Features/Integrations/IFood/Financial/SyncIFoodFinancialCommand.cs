using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Financial;

// Um ciclo de sincronização financeira para UMA empresa — disparado pelo
// IfoodFinancialSyncBackgroundService 1x/dia para cada empresa com integração habilitada
// (Fase 4). Não é chamado diretamente pelo frontend em uso normal — o botão "Sincronizar
// agora" da tela usa o mesmo command pra reenvio manual (carga inicial ou recuperação de falha).
public sealed record SyncIfoodFinancialCommand(long CompanyId) : ICommand;
