using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncBar.Application.Features.Integrations.Ifood.Orders;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Integrations.Ifood;

/// <summary>
/// Loop de polling do módulo Order/Events do Ifood — a cada 30s (recomendação oficial da doc:
/// "comece com polling, migre pra webhook só acima de 1000 pedidos/dia" — não é o caso do
/// SyncBar ainda), para cada empresa com integração habilitada, dispara um ciclo de sincronização
/// (SyncIfoodOrdersCommand). Um BackgroundService é singleton — cria um scope de DI por ciclo
/// pra resolver serviços scoped (DbContext, repositórios, MediatR).
/// </summary>
internal sealed class IfoodOrderPollingBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<IfoodOrderPollingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Pequeno atraso inicial pra deixar a API terminar de subir antes do primeiro ciclo.
        try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ciclo de polling do Ifood falhou inesperadamente.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var settingRepository = scope.ServiceProvider.GetRequiredService<IIfoodIntegrationSettingRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var companyIds = await settingRepository.GetEnabledCompanyIdsAsync(stoppingToken);
        foreach (var companyId in companyIds)
        {
            try
            {
                await mediator.Send(new SyncIfoodOrdersCommand(companyId), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao sincronizar pedidos Ifood da empresa {CompanyId}.", companyId);
            }
        }
    }
}
