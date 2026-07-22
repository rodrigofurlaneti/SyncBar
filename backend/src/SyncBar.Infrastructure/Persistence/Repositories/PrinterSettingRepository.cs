using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class PrinterSettingRepository(AppDbContext context) : IPrinterSettingRepository
{
    public async Task<PrinterSetting?> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.PrinterSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task<PrinterSetting?> GetByBranchForUpdateAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.PrinterSettings
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task AddAsync(PrinterSetting entity, CancellationToken cancellationToken = default)
        => await context.PrinterSettings.AddAsync(entity, cancellationToken);
}
