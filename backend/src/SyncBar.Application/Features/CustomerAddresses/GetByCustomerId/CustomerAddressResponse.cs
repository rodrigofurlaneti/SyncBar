namespace SyncBar.Application.Features.CustomerAddresses.GetByCustomerId
{
    public sealed record CustomerAddressResponse(
        long Id,
        long CompanyId,
        long? BranchId,
        long? CustomerId,
        long? LastOrderId,
        string Street,
        string Number,
        string Supplement,
        DateTime? LastOrderAt,
        bool IsActive,
        DateTime CreatedAt
    );
}
