using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Integrations.IFood;

/// <summary>
/// Watcher de avaliações novas do iFood (Fase 14 — automação candidata nº3 identificada na
/// revisão de documentação da Fase 13). O módulo Review não tem NENHUM evento/webhook — é o
/// único jeito de saber de uma avaliação nova é consultar `GET reviews` periodicamente (auditado
/// contra a coleção Postman oficial "Merchant API — Review" — não existe grupo de evento
/// "review" no módulo Events). Sem este watcher, uma avaliação de nota baixa só é percebida se
/// alguém abrir a tela de Avaliações por conta própria.
///
/// Intervalo de 1 hora (bem mais espaçado que o polling de pedidos/status de loja — avaliação
/// não é uma situação operacional urgente como pedido parado ou loja fora do ar, então não
/// precisa de latência baixa). Mesmo padrão estrutural dos outros watchers deste módulo:
/// singleton, cria um scope de DI por ciclo, estado de "última avaliação vista" só em memória
/// (ConcurrentDictionary por branchId) — se a API reiniciar, o próximo ciclo reconstrói a
/// baseline sem alertar de novo sobre avaliações já vistas antes do restart (mesmo cuidado do
/// watcher de status de loja e do sync financeiro).
/// </summary>
internal sealed class IFoodReviewWatcherBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<IFoodReviewWatcherBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    // Nota de corte pra severidade Warning — abaixo disso (escala 1-5, conforme os exemplos da
    // doc oficial) o alerta chama mais atenção. Acima disso, é só um "Info" de que chegou avaliação.
    private const double LowScoreThreshold = 3;

    // Só em memória, por branchId — maior CreatedAt já visto.
    private readonly ConcurrentDictionary<long, DateTime> _lastSeenReviewCreatedAt = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ciclo do watcher de avaliações iFood falhou inesperadamente.");
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
        var reviewClient = scope.ServiceProvider.GetRequiredService<IIFoodReviewClient>();
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
                logger.LogError(ex, "Falha ao carregar mapeamentos iFood da empresa {CompanyId} no watcher de avaliações.", companyId);
                continue;
            }

            var token = await tokenProvider.GetAccessTokenAsync(companyId, stoppingToken);
            if (token is null)
                continue; // sem token válido — tenta de novo no próximo ciclo

            foreach (var (branchId, mapping) in mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.MerchantId))
                    continue;

                try
                {
                    await CheckBranchAsync(companyId, branchId, mapping.MerchantId!, token, branchRepository, reviewClient, alertStore, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Falha ao verificar avaliações iFood da filial {BranchId}.", branchId);
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
        IIFoodReviewClient reviewClient,
        IIFoodOperationalAlertStore alertStore,
        CancellationToken cancellationToken)
    {
        // Página 1, mais recentes primeiro — suficiente pra detectar novidade a cada 1h; não
        // precisa paginar tudo, só as mais novas desde o último ciclo.
        var result = await reviewClient.GetReviewsAsync(
            accessToken, merchantId, page: 1, pageSize: 20, addCount: false,
            dateFrom: null, dateTo: null, sort: "DESC", sortBy: "CREATED_AT", cancellationToken);

        var reviewsWithDate = result.Reviews.Where(r => r.CreatedAt.HasValue).ToList();
        if (reviewsWithDate.Count == 0)
            return;

        var maxCreatedAt = reviewsWithDate.Max(r => r.CreatedAt!.Value);

        if (!_lastSeenReviewCreatedAt.TryGetValue(branchId, out var lastSeen))
        {
            // Primeiro ciclo desde o boot — só grava a baseline, não alerta sobre avaliações que
            // já existiam antes do SyncBar subir.
            _lastSeenReviewCreatedAt[branchId] = maxCreatedAt;
            return;
        }

        var newReviews = reviewsWithDate.Where(r => r.CreatedAt!.Value > lastSeen).ToList();
        _lastSeenReviewCreatedAt[branchId] = maxCreatedAt > lastSeen ? maxCreatedAt : lastSeen;

        if (newReviews.Count == 0)
            return;

        var branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);
        var branchName = branch?.Name ?? $"Filial {branchId}";

        foreach (var review in newReviews)
        {
            var isLowScore = review.Score.HasValue && review.Score.Value <= LowScoreThreshold;
            var scoreText = review.Score.HasValue ? $"nota {review.Score.Value:0.#}" : "sem nota";
            var comment = review.Comment ?? string.Empty;
            var commentPreview = string.IsNullOrWhiteSpace(comment)
                ? "sem comentário"
                : (comment.Length > 140 ? comment[..140] + "…" : comment);

            alertStore.Raise(
                companyId,
                branchId,
                branchName,
                isLowScore ? "Avaliação nova com nota baixa no iFood" : "Avaliação nova no iFood",
                $"{branchName} recebeu uma avaliação nova ({scoreText}): \"{commentPreview}\" — veja e responda em Integrações > iFood > Avaliações.",
                isLowScore ? IFoodOperationalAlertSeverity.Warning : IFoodOperationalAlertSeverity.Info);
        }
    }
}
