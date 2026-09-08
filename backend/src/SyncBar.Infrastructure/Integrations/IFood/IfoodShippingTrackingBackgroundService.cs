using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence;

namespace SyncBar.Infrastructure.Integrations.Ifood;

internal sealed class IfoodShippingTrackingBackgroundService(
    IServiceScopeFactory scopes, TimeProvider time, ILogger<IfoodShippingTrackingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await PollAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Falha no rastreamento Shipping iFood."); }
        }
    }

    internal async Task PollAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokens = scope.ServiceProvider.GetRequiredService<IIfoodTokenProvider>();
        var client = scope.ServiceProvider.GetRequiredService<IIfoodShippingClient>();
        var now = time.GetUtcNow().UtcDateTime;
        var pending = await db.Set<IfoodShippingTracking>().AsNoTracking()
            .Where(row => row.IsActive && row.NextPollAtUtc <= now).ToListAsync(ct);
        foreach (var row in pending)
        {
            var token = await tokens.GetAccessTokenAsync(row.CompanyId, ct);
            if (string.IsNullOrWhiteSpace(token)) continue;
            now = time.GetUtcNow().UtcDateTime;
            // Reserva atômica compartilhada entre instâncias. A interface só lê o snapshot.
            var acquired = await db.Set<IfoodShippingTracking>()
                .Where(value => value.Id == row.Id && value.IsActive && value.NextPollAtUtc <= now)
                .ExecuteUpdateAsync(update => update.SetProperty(value => value.NextPollAtUtc, now.AddSeconds(30)), ct);
            if (acquired == 0) continue;
            try
            {
                var result = await client.GetTrackingAsync(token, row.OrderId, ct);
                if (!result.Success) { logger.LogWarning("Rastreamento iFood indisponível para {OrderId}: {Error}", row.OrderId, result.ErrorMessage); continue; }
                await db.Set<IfoodShippingTracking>().Where(value => value.Id == row.Id && value.IsActive)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(value => value.Latitude, result.Latitude)
                        .SetProperty(value => value.Longitude, result.Longitude)
                        .SetProperty(value => value.ExpectedDelivery, result.ExpectedDelivery)
                        .SetProperty(value => value.DeliveryEtaEndMinutes, result.DeliveryEtaEndMinutes)
                        .SetProperty(value => value.PickupEtaStartMinutes, result.PickupEtaStartMinutes)
                        .SetProperty(value => value.UpdatedAtUtc, time.GetUtcNow().UtcDateTime), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogWarning(ex, "Falha ao rastrear pedido iFood {OrderId}.", row.OrderId); }
        }
    }
}
