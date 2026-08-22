using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Integrations.IFood;

/// <summary>
/// Watcher de saúde operacional da loja no iFood (Fase 13 — automação encontrada na revisão de
/// documentação pedida em 2026-08-22). Antes desta fase, "operação da loja" (módulo Merchant —
/// disponibilidade, interrupções, horários) só era consultada SOB DEMANDA: alguém precisava abrir
/// a tela de Integrações e clicar em "Atualizar status" pra descobrir que a loja tinha caído do
/// iFood (por exemplo, por um "afastamento automático" do próprio iFood — atrasos/cancelamentos
/// em excesso derrubam a loja e o iFood não avisa por e-mail nem webhook). Esse gap já estava
/// anotado no histórico do projeto desde a Fase 5/9b/9c ("sem polling automático") e é o mesmo
/// tipo de risco que motivou o usuário a tentar criar um worker próprio (ver Fase 12): pedidos
/// perdidos silenciosamente, só que aqui por INDISPONIBILIDADE da loja em vez de evento de pedido
/// não processado.
///
/// Não existe endpoint de evento/webhook pra isso no módulo Events do iFood (auditado nesta
/// mesma revisão — só HANDSHAKE_DISPUTE/HANDSHAKE_SETTLEMENT existem fora do fluxo de pedidos), a
/// única forma de saber é consultar `GET /merchants/{id}/status` periodicamente. Segue o mesmo
/// padrão de BackgroundService dos outros workers do módulo (singleton, cria um scope de DI por
/// ciclo pra resolver dependências scoped) — intervalo de 5 minutos: rápido o suficiente pra
/// avisar em tempo útil, sem bater no rate limit da API (o polling de pedidos já usa 30s pra algo
/// muito mais crítico; status de loja não muda a cada segundo).
///
/// Só gera alerta numa TRANSIÇÃO de estado (disponível → indisponível ou o inverso), nunca a cada
/// ciclo — e o primeiro ciclo depois do boot da API só grava o estado inicial sem alertar, pra não
/// notificar sobre uma condição que já existia antes do SyncBar subir (mesmo cuidado que o
/// dedup de eventos do polling de pedidos já toma). Estado guardado em memória
/// (ConcurrentDictionary, por branchId) — some se a API reiniciar, mas o próximo ciclo reconstrói
/// sozinho em até 5 minutos.
/// </summary>
internal sealed class IFoodMerchantStatusWatcherBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<IFoodMerchantStatusWatcherBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    // Estado só em memória, igual ao alert store — ver comentário na classe sobre o motivo.
    private readonly ConcurrentDictionary<long, bool> _lastKnownAvailable = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Atraso inicial maior que o do polling de pedidos — não é um fluxo crítico de latência,
        // e dá tempo de todo o resto da API (incluindo o cache de token) terminar de subir.
        try { await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ciclo do watcher de status de loja iFood falhou inesperadamente.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var settingRepository = scope.ServiceProvider.GetRequiredService<IIFoodIntegrationSettingRepository>();
        var mappingRepository = scope.ServiceProvider.GetRequiredService<IIFoodMerchantMappingRepository>();
        var branchRepository = scope.ServiceProvider.GetRequiredService<IBranchRepository>();
        var tokenProvider = scope.ServiceProvider.GetRequiredService<IIFoodTokenProvider>();
        var merchantClient = scope.ServiceProvider.GetRequiredService<IIFoodMerchantClient>();
        var alertStore = scope.ServiceProvider.GetRequiredService<IIFoodOperationalAlertStore>();

        var companyIds = await settingRepository.GetEnabledCompanyIdsAsync(stoppingToken);
        foreach (var companyId in companyIds)
        {
            IReadOnlyDictionary<long, IFoodMerchantMapping> mappings;
            try
            {
                mappings = await mappingRepository.GetByCompanyAsync(companyId, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao carregar mapeamentos iFood da empresa {CompanyId} no watcher de status.", companyId);
                continue;
            }

            string? accessToken = null;
            foreach (var (branchId, mapping) in mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.MerchantId))
                    continue; // filial ainda sem MerchantId configurado — nada a checar

                try
                {
                    // Um token por empresa, reaproveitado entre as filiais do ciclo (mesma
                    // instância de IIFoodTokenProvider já cacheia por companyId — isso só evita
                    // uma chamada redundante quando a empresa tem várias filiais no mesmo ciclo).
                    accessToken ??= await tokenProvider.GetAccessTokenAsync(companyId, stoppingToken);
                    if (accessToken is null)
                        break; // sem integração habilitada/token válido — tenta de novo no próximo ciclo

                    await CheckBranchAsync(companyId, branchId, mapping.MerchantId!, accessToken, branchRepository, merchantClient, alertStore, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Falha ao verificar status iFood da filial {BranchId}.", branchId);
                }
            }
        }
    }

    private async Task CheckBranchAsync(
        long companyId,
        long branchId,
        string merchantId,
        string accessToken,
        IBranchRepository branchRepository,
        IIFoodMerchantClient merchantClient,
        IIFoodOperationalAlertStore alertStore,
        CancellationToken cancellationToken)
    {
        var status = await merchantClient.GetStatusAsync(accessToken, merchantId, cancellationToken);
        if (!status.Success)
            return; // falha transitória de rede/API — não é o mesmo que "loja indisponível", não gera alerta

        var branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);
        var branchName = branch?.Name ?? $"Filial {branchId}";

        var wasAvailable = _lastKnownAvailable.TryGetValue(branchId, out var previous) ? previous : (bool?)null;
        _lastKnownAvailable[branchId] = status.Available;

        if (wasAvailable is null)
            return; // primeiro ciclo desde o boot — só grava a baseline, não alerta

        if (wasAvailable.Value && !status.Available)
        {
            var reason = status.Validations.FirstOrDefault()?.Message
                ?? status.OperationState
                ?? "motivo não informado pelo iFood";

            alertStore.Raise(
                companyId,
                branchId,
                branchName,
                "Loja indisponível no iFood",
                $"{branchName} ficou indisponível para pedidos no iFood — {reason}. Verifique em Integrações > iFood > Operação da loja.",
                IFoodOperationalAlertSeverity.Critical);
        }
        else if (!wasAvailable.Value && status.Available)
        {
            alertStore.Raise(
                companyId,
                branchId,
                branchName,
                "Loja voltou a ficar disponível no iFood",
                $"{branchName} voltou a receber pedidos pelo iFood normalmente.",
                IFoodOperationalAlertSeverity.Info);
        }
    }
}
