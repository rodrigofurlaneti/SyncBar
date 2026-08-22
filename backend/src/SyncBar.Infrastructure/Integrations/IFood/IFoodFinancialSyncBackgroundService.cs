using System.Collections.Concurrent;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Financial;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Integrations.IFood;

/// <summary>
/// Loop de sincronização financeira do iFood (Fase 4) — 1x por dia, pra cada empresa com
/// integração habilitada, dispara um ciclo de sincronização (SyncIFoodFinancialCommand). Dados
/// financeiros do iFood atualizam no máximo diariamente (a apuração em si é semanal), então não
/// precisa do polling de 30s usado no módulo de pedidos (Fase 2) — mesmo padrão estrutural de
/// IFoodOrderPollingBackgroundService, só com intervalo bem maior.
///
/// Fase 14 — depois de cada sincronização, reaproveita a mesma verificação de discrepância já
/// usada pela tela "Financeiro" (GetIFoodFinancialSummaryQuery, últimos 30 dias) e publica um
/// alerta (IIFoodOperationalAlertStore, mesmo mecanismo da Fase 13) quando encontra discrepância
/// — antes disso, alguém só descobria abrindo a tela manualmente. Só alerta numa TRANSIÇÃO
/// (sem discrepância → com discrepância, e o inverso quando resolve) pra não repetir o mesmo
/// aviso todo santo dia enquanto ninguém investiga — mesmo cuidado do watcher de status de loja.
/// </summary>
internal sealed class IFoodFinancialSyncBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<IFoodFinancialSyncBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(24);

    // Estado só em memória (por branchId) — mesmo trade-off já aceito pelo watcher de status de
    // loja da Fase 13: perde a baseline se a API reiniciar, mas o próximo ciclo reconstrói.
    private readonly ConcurrentDictionary<long, bool> _lastKnownHasDiscrepancy = new();

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
        var mappingRepository = scope.ServiceProvider.GetRequiredService<IIFoodMerchantMappingRepository>();
        var branchRepository = scope.ServiceProvider.GetRequiredService<IBranchRepository>();
        var alertStore = scope.ServiceProvider.GetRequiredService<IIFoodOperationalAlertStore>();
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
                continue; // sem sync bem-sucedido, não faz sentido checar discrepância desta empresa agora
            }

            await CheckDiscrepancyAlertsAsync(companyId, mappingRepository, branchRepository, alertStore, mediator, stoppingToken);
        }
    }

    private async Task CheckDiscrepancyAlertsAsync(
        long companyId,
        IIFoodMerchantMappingRepository mappingRepository,
        IBranchRepository branchRepository,
        IIFoodOperationalAlertStore alertStore,
        IMediator mediator,
        CancellationToken stoppingToken)
    {
        IReadOnlyDictionary<long, IFoodMerchantMapping> mappings;
        try
        {
            mappings = await mappingRepository.GetByCompanyAsync(companyId, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao carregar mapeamentos iFood da empresa {CompanyId} pra checar discrepância financeira.", companyId);
            return;
        }

        foreach (var (branchId, mapping) in mappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.MerchantId))
                continue;

            try
            {
                // Mesma janela padrão (últimos 30 dias) usada pela tela "Financeiro" quando
                // nenhum período é informado — ver GetIFoodFinancialSummaryQuery.
                var result = await mediator.Send(new GetIFoodFinancialSummaryQuery(branchId, null, null), stoppingToken);
                if (result.IsFailure)
                    continue;

                var hasDiscrepancy = result.Value.HasDiscrepancy;
                var wasKnown = _lastKnownHasDiscrepancy.TryGetValue(branchId, out var previous);
                _lastKnownHasDiscrepancy[branchId] = hasDiscrepancy;

                if (!wasKnown)
                    continue; // primeiro ciclo desde o boot — só grava a baseline, não alerta

                if (hasDiscrepancy && !previous)
                {
                    var branch = await branchRepository.GetByIdAsync(branchId, stoppingToken);
                    var branchName = branch?.Name ?? $"Filial {branchId}";
                    alertStore.Raise(
                        companyId,
                        branchId,
                        branchName,
                        "Discrepância financeira encontrada no iFood",
                        $"{branchName} tem uma discrepância entre eventos financeiros e repasses do iFood " +
                        $"(R$ {result.Value.DiscrepancyAmount:N2}) nos últimos 30 dias — confira em Integrações > iFood > Financeiro.",
                        IFoodOperationalAlertSeverity.Warning);
                }
                else if (!hasDiscrepancy && previous)
                {
                    var branch = await branchRepository.GetByIdAsync(branchId, stoppingToken);
                    var branchName = branch?.Name ?? $"Filial {branchId}";
                    alertStore.Raise(
                        companyId,
                        branchId,
                        branchName,
                        "Discrepância financeira do iFood resolvida",
                        $"{branchName} não apresenta mais discrepância entre eventos financeiros e repasses do iFood nos últimos 30 dias.",
                        IFoodOperationalAlertSeverity.Info);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao checar discrepância financeira da filial {BranchId}.", branchId);
            }
        }
    }
}
