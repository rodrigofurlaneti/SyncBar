using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Catalog;

namespace SyncBar.Infrastructure.Integrations.Ifood;

internal sealed class IfoodCatalogSyncTrigger(IServiceScopeFactory scopeFactory) : IIfoodCatalogSyncTrigger
{
    public void TriggerCompanySync(long companyId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // Escopo próprio (não o da requisição HTTP que disparou isso) — a sincronização
                // faz várias chamadas HTTP pro Ifood e pode demorar mais que o tempo de vida do
                // escopo original. Mesmo padrão usado por IfoodOrderPollingBackgroundService.
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new SyncIfoodCatalogCommand(companyId));
            }
            catch
            {
                // Best-effort: falha aqui não deve derrubar o fluxo que criou/editou o produto —
                // o botão "Sincronizar agora" na tela de integrações cobre o reenvio manual.
            }
        });
    }
}
