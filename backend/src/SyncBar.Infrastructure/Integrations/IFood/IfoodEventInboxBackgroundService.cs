using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence;

namespace SyncBar.Infrastructure.Integrations.Ifood;

internal sealed class IfoodEventInboxBackgroundService(
    IServiceScopeFactory scopes, TimeProvider time, ILogger<IfoodEventInboxBackgroundService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await ProcessAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Falha na fila persistente de eventos iFood."); }
        }
    }

    internal async Task ProcessAsync(CancellationToken ct)
    {
        using var listingScope = scopes.CreateScope();
        var listingDb = listingScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = time.GetUtcNow().UtcDateTime;
        var ids = await listingDb.Set<IfoodEventInbox>().AsNoTracking()
            .Where(row => row.ProcessedAtUtc == null && row.Attempts < 20 && row.NextAttemptAtUtc <= now)
            .OrderBy(row => row.ReceivedAtUtc).ThenBy(row => row.Id).Select(row => row.Id).Take(50).ToListAsync(ct);
        foreach (var id in ids) await ProcessOneAsync(id, ct);
    }

    private async Task ProcessOneAsync(long id, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = time.GetUtcNow().UtcDateTime;
        var leaseUntil = now.AddMinutes(5);
        var acquired = await db.Set<IfoodEventInbox>().Where(row => row.Id == id && row.ProcessedAtUtc == null && row.Attempts < 20 && row.NextAttemptAtUtc <= now)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.NextAttemptAtUtc, leaseUntil)
                .SetProperty(row => row.Attempts, row => row.Attempts + 1), ct);
        if (acquired == 0) return;
        var entry = await db.Set<IfoodEventInbox>().AsNoTracking().SingleAsync(row => row.Id == id, ct);
        string? error = null;
        try
        {
            var evt = JsonSerializer.Deserialize<IfoodPollingEvent>(entry.Payload, JsonOptions)
                ?? throw new JsonException("Evento vazio.");
            var result = await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new SyncIfoodOrdersCommand(entry.CompanyId, [evt]), ct);
            if (result.IsFailure) error = result.Error.Code;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { error = ex.GetType().Name; logger.LogError(ex, "Falha ao processar evento iFood {EventId}.", entry.EventId); }
        // Não salvar entidades rastreadas por um processamento que falhou.
        db.ChangeTracker.Clear();
        var owned = db.Set<IfoodEventInbox>().Where(row => row.Id == id && row.NextAttemptAtUtc == leaseUntil && row.ProcessedAtUtc == null);
        if (error is null)
            await owned.ExecuteUpdateAsync(update => update.SetProperty(row => row.ProcessedAtUtc, time.GetUtcNow().UtcDateTime)
                .SetProperty(row => row.LastError, (string?)null), ct);
        else
        {
            var retryAt = time.GetUtcNow().UtcDateTime.AddSeconds(Math.Min(300, 5 * Math.Pow(2, Math.Min(entry.Attempts, 6))));
            await owned.ExecuteUpdateAsync(update => update.SetProperty(row => row.NextAttemptAtUtc, retryAt)
                .SetProperty(row => row.LastError, error), ct);
            if (entry.Attempts >= 20) logger.LogError("Evento iFood {EventId} requer revisão após 20 tentativas; payload preservado.", entry.EventId);
        }
    }
}
