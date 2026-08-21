using Microsoft.AspNetCore.DataProtection;
using SyncBar.Application.Abstractions.Security;

namespace SyncBar.Infrastructure.Security;

/// <summary>Implementação real de ISecretProtector via ASP.NET Data Protection.</summary>
internal sealed class DataProtectionSecretProtector(IDataProtectionProvider dataProtectionProvider) : ISecretProtector
{
    public string Protect(string purpose, string plaintext) =>
        dataProtectionProvider.CreateProtector(purpose).Protect(plaintext);

    public string Unprotect(string purpose, string protectedValue) =>
        dataProtectionProvider.CreateProtector(purpose).Unprotect(protectedValue);
}
