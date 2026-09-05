namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetByCustomerIdAndCompanyId
{
    public sealed record AsaasIntegrationCustomerResponse(
        long Id,
        long CustomerId,
        long CompanyId,
        string AsaasCustomerId,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive);
}
