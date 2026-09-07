using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.OrderOrigin.ExistsByName
{
    public sealed record ExistsOrderOriginByNameQuery(
        long? CompanyId,
        long? BranchId,
        string Name,
        long? ExcludeId = null) : IQuery<bool>;
}
