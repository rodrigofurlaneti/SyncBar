using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class ServiceFeeSettingRepository(AppDbContext context) : IServiceFeeSettingRepository
{
    public async Task<ServiceFeeSetting?> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.ServiceFeeSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task<ServiceFeeSetting?> GetByBranchForUpdateAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.ServiceFeeSettings
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task AddAsync(ServiceFeeSetting entity, CancellationToken cancellationToken = default)
        => await context.ServiceFeeSettings.AddAsync(entity, cancellationToken);
}
