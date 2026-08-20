using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncBar.Application.Features.Integrations.IFood.Financial;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Integrations.IFood;

/// <summary>
/// Loop de sincronização financeira do iFood (Fase 4) — 1x por dia, pra cada empresa com
/// integração habilitada, dispara um ciclo de sincronização (SyncIFoodFinancialCommand). Dados
/// financeiros do iFood atualizam no máximo diariamente (a apuração em si é semanal), então não
/// precisa do polling de 30s usado no módulo de pedidos (Fase 2) — mesmo padrão estrutural de
/// IFoodOrderPollingBackgroundService, só com intervalo bem maior.
/// </summary>
internal sealed class IFoodFinancialSyncBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<IFoodFinancialSyncBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Atraso inicial maior que o do polling de pedidos — não é urgente rodar logo no boot,
        // e evita concorrer com outros hosted services subindo ao mesmo tempo.
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ciclo de sincronização financeira do iFood falhou inesperadamente.");
            }

            try { await Task.Delay(SyncInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var settingRepository = scope.ServiceProvider.GetRequiredService<IIFoodIntegrationSettingRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var companyIds = await settingRepository.GetEnabledCompanyIdsAsync(stoppingToken);
        foreach (var companyId in companyIds)
        {
            try
            {
                await mediator.Send(new SyncIFoodFinancialCommand(companyId), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao sincronizar financeiro iFood da empresa {CompanyId}.", companyId);
            }
        }
    }
}
