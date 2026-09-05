namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId
{
    public sealed record AsaasIntegrationCustomerResponse(
        long Id,
        long CustomerId,
        long CompanyId,
        string AsaasCustomerId,
        DateTime CreatedAt,
        bool IsActive);
}
