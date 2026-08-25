using SyncBar.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyncBar.Domain.Repositories
{
    public interface IDiningAreaTableRepository
    {
        Task<DiningAreaTable?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<IEnumerable<DiningAreaTable>> GetByDiningAreaIdAsync(long diningAreaId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByTableIdAsync(long diningTableId, CancellationToken cancellationToken = default);
        Task<DiningAreaTable?> GetByTableIdAsync(long diningTableId, CancellationToken cancellationToken = default);
        Task AddAsync(DiningAreaTable entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(DiningAreaTable entity, CancellationToken cancellationToken = default);
    }
}