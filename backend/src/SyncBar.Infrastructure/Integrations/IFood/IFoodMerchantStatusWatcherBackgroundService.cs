using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Integrations.Ifood;

/// <summary>
/// Watcher de saúde operacional da loja no Ifood (Fase 13 — automação encontrada na revisão de
/// documentação pedida em 2026-08-22). Antes desta fase, "operação da loja" (módulo Merchant —
/// disponibilidade, interrupções, horários) só era consultada SOB DEMANDA: alguém precisava abrir
/// a tela de Integrações e clicar em "Atualizar status" pra descobrir que a loja tinha caído do
/// Ifood (por exemplo, por um "afastamento automático" do próprio Ifood — atrasos/cancelamentos
/// em excesso derrubam a loja e o Ifood não avisa por e-mail nem webhook). Esse gap já estava
/// anotado no histórico do projeto desde a Fase 5/9b/9c ("sem polling automático") e é o mesmo
/// tipo de risco que motivou o usuário a tentar criar um worker próprio (ver Fase 12): pedidos
/// perdidos silenciosamente, só que aqui por INDISPONIBILIDADE da loja em vez de evento de pedido
/// não processado.
///
/// Não existe endpoint de evento/webhook pra isso no módulo Events do Ifood (auditado nesta
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
///
/// Correção pós-revisão (CodeRabbit, PR #4): a transição usava só `status.Available`, então uma
/// loja fechada normalmente fora do horário de funcionamento (fim de expediente) virava um
/// "Loja indisponível" Crítico, e a reabertura seguinte virava um "voltou a ficar disponível" —
/// ruído todo santo dia. Não dá pra distinguir isso usando `OperationState`/`Validations` do
/// próprio Ifood (o vocabulário exato desses campos NUNCA foi confirmado contra uma resposta real
/// de sandbox — ver ressalva em IIfoodMerchantClient — então filtrar por um texto tipo "CLOSED"
/// seria adivinhação, não uma correção). Em vez disso, usa `IfoodOpeningHours` — a cópia local dos
/// turnos de funcionamento que o próprio SyncBar mantém sincronizada com o Ifood (`PUT
/// /opening-hours`, Fase 5) — como fonte confiável: se a filial tem turnos configurados e o
/// momento da checagem está FORA de todos eles, o fechamento é esperado e não gera alerta (nem a
/// reabertura seguinte). Se a filial não tem nenhum turno configurado, não há como classificar —
/// mantém o comportamento anterior (alerta sempre), mesmo default conservador já usado no resto do
/// arquivo quando falta dado confiável.
/// </summary>
internal sealed class IfoodMerchantStatusWatcherBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<IfoodMerchantStatusWatcherBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    // Estado só em memória, igual ao alert store — ver comentário na classe sobre o motivo.
    private readonly ConcurrentDictionary<long, bool> _lastKnownAvailable = new();

    // Guarda, por filial, se a última transição pra indisponível foi classificada como
    // "fechamento esperado" (fora do horário configurado) — usado só pra decidir se a transição
    // de volta pra disponível também deve ser silenciada (reabertura normal, não uma recuperação).
    private readonly ConcurrentDictionary<long, bool> _unavailableWasExpectedClosure = new();

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
                logger.LogError(ex, "Ciclo do watcher de status de loja Ifood falhou inesperadamente.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var settingRepository = scope.ServiceProvider.GetRequiredService<IIfoodIntegrationSettingRepository>();
        var mappingRepository = scope.ServiceProvider.GetRequiredService<IIfoodMerchantMappingRepository>();
        var branchRepository = scope.ServiceProvider.GetRequiredService<IBranchRepository>();
        var tokenProvider = scope.ServiceProvider.GetRequiredService<IIfoodTokenProvider>();
        var merchantClient = scope.ServiceProvider.GetRequiredService<IIfoodMerchantClient>();
        var alertStore = scope.ServiceProvider.GetRequiredService<IIfoodOperationalAlertStore>();
        var openingHoursRepository = scope.ServiceProvider.GetRequiredService<IIfoodOpeningHoursRepository>();

        var companyIds = await settingRepository.GetEnabledCompanyIdsAsync(stoppingToken);
        foreach (var companyId in companyIds)
        {
            IReadOnlyDictionary<long, IfoodMerchantMapping> mappings;
            try
            {
                mappings = await mappingRepository.GetByCompanyAsync(companyId, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao carregar mapeamentos Ifood da empresa {CompanyId} no watcher de status.", companyId);
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
                    // instância de IIfoodTokenProvider já cacheia por companyId — isso só evita
                    // uma chamada redundante quando a empresa tem várias filiais no mesmo ciclo).
                    accessToken ??= await tokenProvider.GetAccessTokenAsync(companyId, stoppingToken);
                    if (accessToken is null)
                        break; // sem integração habilitada/token válido — tenta de novo no próximo ciclo

                    await CheckBranchAsync(companyId, branchId, mapping.MerchantId!, accessToken, branchRepository, merchantClient, alertStore, openingHoursRepository, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Falha ao verificar status Ifood da filial {BranchId}.", branchId);
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
        IIfoodMerchantClient merchantClient,
        IIfoodOperationalAlertStore alertStore,
        IIfoodOpeningHoursRepository openingHoursRepository,
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
            var shifts = await openingHoursRepository.GetByBranchAsync(branchId, cancellationToken);
            if (shifts.Count > 0 && !IsWithinConfiguredShift(shifts, DateTime.Now))
            {
                // Fechamento esperado (fora do horário de funcionamento configurado pela própria
                // filial) — não é uma indisponibilidade acionável. Ver comentário na classe.
                _unavailableWasExpectedClosure[branchId] = true;
                return;
            }

            _unavailableWasExpectedClosure[branchId] = false;

            var reason = status.Validations.FirstOrDefault()?.Message
                ?? status.OperationState
                ?? "motivo não informado pelo Ifood";

            alertStore.Raise(
                companyId,
                branchId,
                branchName,
                "Loja indisponível no Ifood",
                $"{branchName} ficou indisponível para pedidos no Ifood — {reason}. Verifique em Integrações > Ifood > Operação da loja.",
                IfoodOperationalAlertSeverity.Critical);
        }
        else if (!wasAvailable.Value && status.Available)
        {
            var wasExpectedClosure = _unavailableWasExpectedClosure.TryRemove(branchId, out var expected) && expected;
            if (wasExpectedClosure)
                return; // reabertura normal depois de um fechamento esperado — não é uma "recuperação"

            alertStore.Raise(
                companyId,
                branchId,
                branchName,
                "Loja voltou a ficar disponível no Ifood",
                $"{branchName} voltou a receber pedidos pelo Ifood normalmente.",
                IfoodOperationalAlertSeverity.Info);
        }
    }

    // Turnos em horário local do servidor — mesma convenção já usada em IfoodOpeningHours
    // (CreatedAt = DateTime.Now, não Now). Cobre turno que cruza a meia-noite (ex.: 22h–2h).
    private static bool IsWithinConfiguredShift(IReadOnlyCollection<IfoodOpeningHours> shifts, DateTime now)
    {
        var nowTimeOfDay = now.TimeOfDay;
        var nowDayOfWeek = (int)now.DayOfWeek;

        foreach (var shift in shifts)
        {
            if (ShiftCoversMoment(shift, nowDayOfWeek, nowTimeOfDay))
                return true;
        }

        return false;
    }

    private static bool ShiftCoversMoment(IfoodOpeningHours shift, int nowDayOfWeek, TimeSpan nowTimeOfDay)
    {
        var end = shift.Start + TimeSpan.FromMinutes(shift.DurationMinutes);

        return end <= TimeSpan.FromDays(1)
            ? CoversSameDayShift(shift, end, nowDayOfWeek, nowTimeOfDay)
            : CoversOvernightShift(shift, end, nowDayOfWeek, nowTimeOfDay);
    }

    private static bool CoversSameDayShift(IfoodOpeningHours shift, TimeSpan end, int nowDayOfWeek, TimeSpan nowTimeOfDay)
        => shift.DayOfWeek == nowDayOfWeek && nowTimeOfDay >= shift.Start && nowTimeOfDay < end;

    // Turno cruza a meia-noite: cobre o restante do dia em que começa e o início do dia
    // seguinte até a hora de término (já normalizada pra menos de 24h).
    private static bool CoversOvernightShift(IfoodOpeningHours shift, TimeSpan end, int nowDayOfWeek, TimeSpan nowTimeOfDay)
    {
        if (shift.DayOfWeek == nowDayOfWeek && nowTimeOfDay >= shift.Start)
            return true;

        var endDayOfWeek = (shift.DayOfWeek + 1) % 7;
        var endTimeOfDayNextDay = end - TimeSpan.FromDays(1);
        return endDayOfWeek == nowDayOfWeek && nowTimeOfDay < endTimeOfDayNextDay;
    }
}
