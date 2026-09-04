namespace SyncBar.Application.Features.CustomerAddresses.GetByBranchId
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
        string ZipCode,
        DateTime? LastOrderAt,
        bool IsActive,
        DateTime CreatedAt
    );
}
