using SyncBar.Domain.Entities;
namespace SyncBar.Domain.Repositories
{
    public interface IComandaItemTransferRepository
    {
        Task AddAsync(ComandaItemTransfer entity, CancellationToken cancellationToken = default);
    }
}
