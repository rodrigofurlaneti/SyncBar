using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IShiftClosingSessionRepository
{
    Task<IReadOnlyCollection<ShiftClosingSession>> GetByShiftClosingAsync(long shiftClosingId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<ShiftClosingSession> entities, CancellationToken cancellationToken = default);
}
