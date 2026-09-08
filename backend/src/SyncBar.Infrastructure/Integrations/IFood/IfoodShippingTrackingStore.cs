using Microsoft.EntityFrameworkCore;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence;

namespace SyncBar.Infrastructure.Integrations.Ifood;

internal sealed class IfoodShippingTrackingStore(AppDbContext db, TimeProvider time) : IIfoodShippingTrackingStore
{
    public async Task<bool> ApplyEventAsync(long companyId, IfoodPollingEvent evt, CancellationToken ct)
    {
        var code = evt.FullCode ?? evt.Code;
        if (code is not ("ASSIGN_DRIVER" or "REQUEST_DRIVER_SUCCESS" or "DELIVERED" or "CONCLUDED" or "CANCELLED")) return false;
        var row = await db.Set<IfoodShippingTracking>().SingleOrDefaultAsync(value => value.CompanyId == companyId && value.OrderId == evt.OrderId, ct);
        if (code is "DELIVERED" or "CONCLUDED" or "CANCELLED")
        {
            if (row is null) return false;
            row.Stop();
            await db.SaveChangesAsync(ct);
            return true;
        }
        if (row is not null) return true;

        var branchId = await db.Set<IfoodOrder>().Where(value => value.IfoodOrderId == evt.OrderId && value.IsActive)
            .Select(value => (long?)value.BranchId).FirstOrDefaultAsync(ct)
            ?? await db.Set<IfoodShippingDelivery>().Where(value => value.IfoodDeliveryId == evt.OrderId && value.IsActive)
                .Select(value => (long?)value.BranchId).FirstOrDefaultAsync(ct);
        if (branchId is null || !await db.Set<Branch>().AnyAsync(value => value.Id == branchId && value.CompanyId == companyId, ct)) return false;
        db.Add(IfoodShippingTracking.Assigned(companyId, branchId.Value, evt.OrderId, time.GetUtcNow().UtcDateTime));
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IfoodShippingTrackingResult> ReadAsync(long companyId, string orderId, CancellationToken ct)
    {
        var row = await db.Set<IfoodShippingTracking>().AsNoTracking()
            .SingleOrDefaultAsync(value => value.CompanyId == companyId && value.OrderId == orderId, ct);
        return new(true, null, row?.Latitude, row?.Longitude, row?.ExpectedDelivery, row?.DeliveryEtaEndMinutes, row?.PickupEtaStartMinutes);
    }
}
