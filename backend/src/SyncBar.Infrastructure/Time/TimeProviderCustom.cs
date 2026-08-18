using System;

namespace SyncBar.Infrastructure.Time
{
    public class TimeProviderCustom : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}