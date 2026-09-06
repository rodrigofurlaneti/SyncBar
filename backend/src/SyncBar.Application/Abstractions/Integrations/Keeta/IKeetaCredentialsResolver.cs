namespace SyncBar.Application.Abstractions.Integrations.Keeta
{
    /// <summary>
    /// Resolve as credenciais Keeta (BaseUrl + ClientId + ClientSecret + AppId) a usar para uma
    /// empresa/filial: prioriza a configuração cadastrada em KeetaIntegrationSetting (filial, depois
    /// empresa); se não houver nenhuma linha ativa configurada, cai para os valores do appsettings.
    /// </summary>
    public interface IKeetaCredentialsResolver
    {
        Task<KeetaCredentials> ResolveAsync(long companyId, long branchId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve usando apenas o appsettings, sem escopo de empresa/filial — usado quando uma
        /// sessão de autorização ainda não foi correlacionada a nenhum tenant (ex.: webhook de
        /// autorização chegou antes do callback do navegador).
        /// </summary>
        Task<KeetaCredentials> ResolveDefaultAsync(CancellationToken cancellationToken = default);
    }
}
