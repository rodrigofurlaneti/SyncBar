using Microsoft.EntityFrameworkCore;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence;

namespace SyncBar.Infrastructure.Integrations.Ifood;

internal sealed class IfoodEventInboxStore(AppDbContext db, TimeProvider time) : IIfoodEventInbox
{
    public async Task EnqueueAsync(long companyId, string eventId, string payload, CancellationToken ct)
    {
        if (await db.Set<IfoodEventInbox>().AnyAsync(row => row.CompanyId == companyId && row.EventId == eventId, ct)) return;
        var entry = IfoodEventInbox.Receive(companyId, eventId, payload, time.GetUtcNow().UtcDateTime);
        db.Add(entry);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            db.Entry(entry).State = EntityState.Detached;
            if (!await db.Set<IfoodEventInbox>().AnyAsync(row => row.CompanyId == companyId && row.EventId == eventId, ct)) throw;
        }
    }
}
