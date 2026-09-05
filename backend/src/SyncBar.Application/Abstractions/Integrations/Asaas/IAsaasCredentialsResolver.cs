namespace SyncBar.Application.Abstractions.Integrations.Asaas
{
    /// <summary>
    /// Resolve as credenciais Asaas (BaseUrl + ApiKey) a usar para uma empresa/filial:
    /// prioriza a configuração cadastrada em AsaasIntegrationSetting (filial, depois empresa);
    /// se não houver nenhuma linha ativa configurada, cai para os valores do appsettings.
    /// </summary>
    public interface IAsaasCredentialsResolver
    {
        Task<AsaasCredentials> ResolveAsync(long companyId, long? branchId, CancellationToken cancellationToken = default);
    }
}
