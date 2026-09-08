using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Persistence;

namespace SyncBar.Infrastructure.Integrations.Ifood;

public sealed class IfoodAnalyticsExtractionOptions
{
    public bool Enabled { get; set; }
}

internal sealed class IfoodAnalyticsExtractionBackgroundService(
    IServiceScopeFactory scopes, IOptions<IfoodAnalyticsExtractionOptions> options,
    TimeProvider time, ILogger<IfoodAnalyticsExtractionBackgroundService> logger) : BackgroundService
{
    private static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    internal static DateTime LocalNow(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, Brasilia).DateTime;
    internal static bool SnapshotAvailable(DateTimeOffset utc) => LocalNow(utc).TimeOfDay >= new TimeSpan(9, 5, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do
        {
            if (!SnapshotAvailable(time.GetUtcNow())) continue;
            try { await ExtractAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Falha na extração Analytics iFood. Snapshots anteriores preservados."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExtractAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = scope.ServiceProvider.GetRequiredService<IIfoodIntegrationSettingRepository>();
        var mappings = scope.ServiceProvider.GetRequiredService<IIfoodMerchantMappingRepository>();
        var tokens = scope.ServiceProvider.GetRequiredService<IIfoodTokenProvider>();
        var client = scope.ServiceProvider.GetRequiredService<IIfoodAnalyticsClient>();
        var today = LocalNow(time.GetUtcNow()).Date;
        foreach (var companyId in await settings.GetEnabledCompanyIdsAsync(ct))
        {
            var token = await tokens.GetAccessTokenAsync(companyId, ct);
            if (string.IsNullOrWhiteSpace(token)) continue;
            ValidateMerchantScopes(token);
            foreach (var mapping in (await mappings.GetByCompanyAsync(companyId, ct)).Values.Where(row => row.IsActive && !string.IsNullOrWhiteSpace(row.MerchantId)))
            {
                for (var daysAgo = 1; daysAgo <= 15; daysAgo++)
                {
                    var date = today.AddDays(-daysAgo);
                    var row = await db.Set<IfoodAnalyticsSnapshot>().SingleOrDefaultAsync(value => value.BranchId == mapping.BranchId && value.MerchantId == mapping.MerchantId && value.ReferenceDate == date, ct);
                    if (row is not null && LocalNow(new DateTimeOffset(DateTime.SpecifyKind(row.RefreshedAtUtc, DateTimeKind.Utc))).Date == today) continue;
                    var buckets = new List<JsonElement>();
                    for (var page = 1; page <= 1000; page++)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(150), ct);
                        var result = await client.GetOrderKpisAsync(token, mapping.MerchantId!, date, date, page, 100, ct);
                        foreach (var raw in result.RawBuckets)
                        {
                            using var document = JsonDocument.Parse(raw);
                            buckets.Add(document.RootElement.Clone());
                        }
                        if (result.RawBuckets.Count < 100) break;
                        if (page == 1000) throw new InvalidOperationException("Limite de paginação Analytics excedido; snapshot não substituído.");
                    }
                    if (row is null)
                    {
                        row = IfoodAnalyticsSnapshot.Create(mapping.BranchId, mapping.MerchantId!, date);
                        db.Add(row);
                    }
                    row.Replace(JsonSerializer.Serialize(buckets), time.GetUtcNow().UtcDateTime);
                    await db.SaveChangesAsync(ct);
                }
            }
        }
    }

    internal static void ValidateMerchantScopes(string token)
    {
        try
        {
            var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload.PadRight((payload.Length + 3) / 4 * 4, '=')));
            var scope = document.RootElement.GetProperty("scope");
            var values = scope.ValueKind == JsonValueKind.Array
                ? scope.EnumerateArray().Select(item => item.GetString() ?? "").ToHashSet(StringComparer.Ordinal)
                : (scope.GetString() ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
            if (values.Contains("analytics") && values.Contains("merchant_scope") && !values.Contains("chain_scope")) return;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or IndexOutOfRangeException or KeyNotFoundException or InvalidOperationException) { }
        throw new InvalidOperationException("Analytics exige analytics + merchant_scope neste app. Não misture chain_scope; confira a configuração do app iFood.");
    }
}
