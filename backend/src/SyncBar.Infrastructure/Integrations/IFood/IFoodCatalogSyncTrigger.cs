using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Catalog;

namespace SyncBar.Infrastructure.Integrations.IFood;

internal sealed class IFoodCatalogSyncTrigger(IServiceScopeFactory scopeFactory) : IIFoodCatalogSyncTrigger
{
    public void TriggerCompanySync(long companyId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // Escopo próprio (não o da requisição HTTP que disparou isso) — a sincronização
                // faz várias chamadas HTTP pro iFood e pode demorar mais que o tempo de vida do
                // escopo original. Mesmo padrão usado por IFoodOrderPollingBackgroundService.
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new SyncIFoodCatalogCommand(companyId));
            }
            catch
            {
                // Best-effort: falha aqui não deve derrubar o fluxo que criou/editou o produto —
                // o botão "Sincronizar agora" na tela de integrações cobre o reenvio manual.
            }
        });
    }
}
