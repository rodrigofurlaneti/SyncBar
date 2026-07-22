using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IPrinterSettingRepository
{
    Task<PrinterSetting?> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task<PrinterSetting?> GetByBranchForUpdateAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(PrinterSetting entity, CancellationToken cancellationToken = default);
}
