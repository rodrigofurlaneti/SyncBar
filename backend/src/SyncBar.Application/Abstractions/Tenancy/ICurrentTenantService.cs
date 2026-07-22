namespace SyncBar.Application.Abstractions.Tenancy;

/// <summary>
/// Resolve a empresa (Company) do usuário autenticado na requisição atual.
/// Implementado na Infrastructure a partir da claim "companyId" do JWT.
/// Usado pelo AppDbContext para aplicar isolamento multi-tenant via global query filters.
/// </summary>
public interface ICurrentTenantService
{
    /// <summary>
    /// CompanyId do usuário autenticado, ou null quando não há usuário autenticado
    /// (ex.: login, refresh, jobs de background, migrations/seed).
    /// Quando null, os filtros de tenant NÃO são aplicados — use com cuidado fora de request HTTP.
    /// </summary>
    long? CompanyId { get; }
}
