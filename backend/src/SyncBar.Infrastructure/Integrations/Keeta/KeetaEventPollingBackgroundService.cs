using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncBar.Application.Features.Integrations.Keeta.Order.Polling;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Integrations.Keeta;

/// <summary>
/// Loop de polling do módulo de eventos da Keeta (GET /v1/events:polling) — a cada 30s (mesmo
/// intervalo usado pelo polling do Ifood), agrupa os merchants autorizados por empresa/filial e
/// dispara um ciclo de PollKeetaEventsCommand por escopo de tenant (cada um usa seu próprio
/// access_token/credenciais). Um BackgroundService é singleton — cria um scope de DI por ciclo
/// pra resolver serviços scoped (DbContext, repositórios, MediatR).
/// </summary>
internal sealed class KeetaEventPollingBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<KeetaEventPollingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                logger.LogError(ex, "Ciclo de polling de eventos da Keeta falhou inesperadamente.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var mappingRepository = scope.ServiceProvider.GetRequiredService<IKeetaIntegrationMerchantMappingRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var authorizedMappings = await mappingRepository.GetAllAuthorizedAsync(stoppingToken);

        var scopes = authorizedMappings
            .GroupBy(m => (m.CompanyId, m.BranchId))
            .Select(g => (g.Key.CompanyId, g.Key.BranchId, MerchantIds: g.Select(m => m.InternalMerchantId).ToList()));

        foreach (var (companyId, branchId, merchantIds) in scopes)
        {
            try
            {
                await mediator.Send(new PollKeetaEventsCommand(companyId, branchId, merchantIds), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao sincronizar eventos Keeta da empresa {CompanyId}/filial {BranchId}.", companyId, branchId);
            }
        }
    }
}
