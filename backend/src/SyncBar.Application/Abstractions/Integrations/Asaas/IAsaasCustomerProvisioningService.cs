using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;

namespace SyncBar.Application.Abstractions.Integrations.Asaas
{
    /// <summary>
    /// Garante que um Customer local tenha um vínculo AsaasIntegrationCustomer, criando-o no
    /// gateway (e persistindo o vínculo) quando ainda não existir. Compartilhado pelos fluxos de
    /// checkout (Pix, Cartão) para não duplicar a lógica de "verificar/criar cliente no Asaas".
    /// </summary>
    public interface IAsaasCustomerProvisioningService
    {
        Task<Result<string>> EnsureLinkedAsync(Customer customer, long companyId, long? branchId, CancellationToken cancellationToken = default);
    }
}
