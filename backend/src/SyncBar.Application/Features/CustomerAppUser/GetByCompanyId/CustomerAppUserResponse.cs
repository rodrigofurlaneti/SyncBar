namespace SyncBar.Application.Features.CustomerAppUser.GetByCompanyId
{
    public sealed record CustomerAppUserResponse(
        long Id,
        long CompanyId,
        long? BranchId,
        long? CustomerId,
        string UserName,
        string Email,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? LastLoginAt
    );
}
