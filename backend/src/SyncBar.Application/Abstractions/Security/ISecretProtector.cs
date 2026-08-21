namespace SyncBar.Application.Abstractions.Security;

/// <summary>
/// Abstração pra cifrar/decifrar segredos antes de persistir (ex.: ClientSecret de integrações
/// externas). A implementação real (Infrastructure.Security.DataProtectionSecretProtector) usa
/// ASP.NET Data Protection — mas a Application NÃO pode referenciar Microsoft.AspNetCore.*
/// diretamente (viola a regra de dependência do Clean Architecture e, na prática, nem compila:
/// o projeto Application é uma class library "pura", sem o framework do ASP.NET Core
/// disponível). Mesmo padrão de IPasswordHasher/IJwtTokenProvider: interface aqui, implementação
/// concreta na Infrastructure, registrada em SyncBar.Infrastructure.DependencyInjection.
///
/// <paramref name="purpose"/> isola criptograficamente segredos de features diferentes (ex.:
/// "SyncBar.Integrations.IFood.ClientSecret.v1") — use uma string fixa e nunca a troque depois
/// que já houver segredos salvos com ela, ou eles ficam ilegíveis.
/// </summary>
public interface ISecretProtector
{
    string Protect(string purpose, string plaintext);
    string Unprotect(string purpose, string protectedValue);
}
