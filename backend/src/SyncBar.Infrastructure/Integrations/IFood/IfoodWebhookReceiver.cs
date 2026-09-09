using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Security;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Integrations.Ifood;

internal sealed class IfoodWebhookReceiver(
    IIfoodIntegrationSettingRepository settings, IIfoodMerchantMappingRepository mappings,
    ISecretProtector protector, IIfoodEventInbox inbox) : IIfoodWebhookReceiver
{
    internal static bool Verify(byte[] body, string secret, string? signature)
    {
        if (signature is null || signature.Length != 64) return false;
        byte[] supplied;
        try { supplied = Convert.FromHexString(signature); }
        catch (FormatException) { return false; }
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
        return CryptographicOperations.FixedTimeEquals(expected, supplied);
    }

    public async Task<IfoodWebhookReceipt> ReceiveAsync(byte[] body, string? signature, CancellationToken ct)
    {
        if (signature is null || signature.Length != 64) return new(401);
        var authenticatedCompanies = new List<long>();
        var hasUsableConfiguration = false;
        foreach (var companyId in await settings.GetEnabledCompanyIdsAsync(ct))
        {
            var setting = await settings.GetByCompanyAsync(companyId, ct);
            if (setting is null || !setting.IsActive || !setting.Enabled || setting.EventDeliveryMode != "Webhook"
                || string.IsNullOrWhiteSpace(setting.ClientSecretEncrypted)) continue;
            try
            {
                var secret = protector.Unprotect("SyncBar.Integrations.Ifood.ClientSecret.v1", setting.ClientSecretEncrypted);
                hasUsableConfiguration = true;
                if (Verify(body, secret, signature)) authenticatedCompanies.Add(companyId);
            }
            catch (CryptographicException) { }
        }
        if (authenticatedCompanies.Count == 0) return new(hasUsableConfiguration ? 401 : 503);
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return new(400);
            var code = String(root, "fullCode") ?? String(root, "code");
            if (string.IsNullOrWhiteSpace(code)) return new(400);
            if (code == "KEEPALIVE")
            {
                // A shared application can serve multiple companies. Merge only authenticated presence.
                var merchants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var accepted = false;
                foreach (var companyId in authenticatedCompanies)
                {
                    var receipt = await ReceiveAsync(companyId, body, signature, ct);
                    if (receipt.StatusCode == 400) return receipt;
                    if (receipt.StatusCode != 202) continue;
                    accepted = true;
                    if (receipt.MerchantIds is not null) merchants.UnionWith(receipt.MerchantIds);
                }
                return new(accepted ? 202 : 503, root.TryGetProperty("merchantIds", out _) ? merchants.ToArray() : null);
            }
            var merchantId = String(root, "merchantId");
            if (string.IsNullOrWhiteSpace(merchantId)) return new(400);
            var owners = new List<long>();
            foreach (var companyId in authenticatedCompanies)
            {
                var merchantMappings = await mappings.GetByCompanyAsync(companyId, ct);
                if (merchantMappings.Values.Any(mapping => mapping.IsActive
                    && string.Equals(mapping.MerchantUuid, merchantId, StringComparison.OrdinalIgnoreCase)))
                    owners.Add(companyId);
            }
            // Never choose an arbitrary tenant or enqueue twice for an ambiguous mapping.
            if (owners.Count != 1) return new(owners.Count == 0 ? 403 : 503);
            return await ReceiveAsync(owners[0], body, signature, ct);
        }
        catch (JsonException) { return new(400); }
    }

    public async Task<IfoodWebhookReceipt> ReceiveAsync(long companyId, byte[] body, string? signature, CancellationToken ct)
    {
        var setting = await settings.GetByCompanyAsync(companyId, ct);
        if (setting is null || !setting.IsActive || !setting.Enabled || setting.EventDeliveryMode != "Webhook") return new(503);
        if (string.IsNullOrWhiteSpace(setting.ClientSecretEncrypted)) return new(503);
        string secret;
        try { secret = protector.Unprotect("SyncBar.Integrations.Ifood.ClientSecret.v1", setting.ClientSecretEncrypted); }
        catch (CryptographicException) { return new(503); }
        if (!Verify(body, secret, signature)) return new(401);
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return new(400);
            var code = String(root, "fullCode") ?? String(root, "code");
            if (string.IsNullOrWhiteSpace(code)) return new(400);
            var merchantMappings = (await mappings.GetByCompanyAsync(companyId, ct)).Values
                .Where(value => value.IsActive && !string.IsNullOrWhiteSpace(value.MerchantUuid))
                .Select(value => value.MerchantUuid!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (code == "KEEPALIVE")
            {
                if (!root.TryGetProperty("merchantIds", out var merchantIds))
                    return new(merchantMappings.Count > 0 ? 202 : 503);
                if (merchantIds.ValueKind != JsonValueKind.Array || merchantIds.GetArrayLength() > 1000) return new(400);
                if (merchantIds.EnumerateArray().Any(value => value.ValueKind != JsonValueKind.String)) return new(400);
                return new(202, merchantIds.EnumerateArray().Select(value => value.GetString()!)
                    .Where(merchantMappings.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
            }
            var id = String(root, "id");
            var orderId = String(root, "orderId");
            var merchantId = String(root, "merchantId");
            if (string.IsNullOrWhiteSpace(id) || id.Length > 100 || string.IsNullOrWhiteSpace(orderId) ||
                !root.TryGetProperty("createdAt", out var createdAt) || createdAt.ValueKind != JsonValueKind.String ||
                !createdAt.TryGetDateTimeOffset(out _)) return new(400);
            if (merchantId is null || !merchantMappings.Contains(merchantId)) return new(403);
            await inbox.EnqueueAsync(companyId, id, Encoding.UTF8.GetString(body), ct);
            return new(202);
        }
        catch (JsonException) { return new(400); }
    }

    private static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
