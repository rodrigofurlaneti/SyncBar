using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using SyncBar.Application.Abstractions.Security;

namespace SyncBar.Infrastructure.Authentication;

public sealed class ReadingProofService(IDataProtectionProvider provider, TimeProvider clock) : IReadingProofService
{
    private readonly IDataProtector _protector = provider.CreateProtector("SyncBar.PublicReading.v1");
    public string Issue(Guid tableToken, string? comandaCode, string method) => _protector.Protect(
        JsonSerializer.Serialize(new Proof(tableToken, comandaCode ?? "", method.ToLowerInvariant(), clock.GetUtcNow().AddMinutes(30))));

    public bool Validate(string? proof, Guid tableToken, string? comandaCode, IReadOnlyCollection<string> allowedMethods)
    {
        if (string.IsNullOrWhiteSpace(proof)) return false;
        try
        {
            var value = JsonSerializer.Deserialize<Proof>(_protector.Unprotect(proof));
            return value is not null && value.TableToken == tableToken && value.ComandaCode == (comandaCode ?? "")
                && value.ExpiresAt > clock.GetUtcNow() && allowedMethods.Contains(value.Method);
        }
        catch (CryptographicException) { return false; }
        catch (JsonException) { return false; }
    }

    private sealed record Proof(Guid TableToken, string ComandaCode, string Method, DateTimeOffset ExpiresAt);
}
