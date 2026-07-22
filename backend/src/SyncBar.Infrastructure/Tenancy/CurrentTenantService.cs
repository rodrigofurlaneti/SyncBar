using Microsoft.AspNetCore.Http;
using SyncBar.Application.Abstractions.Tenancy;

namespace SyncBar.Infrastructure.Tenancy;

internal sealed class CurrentTenantService(IHttpContextAccessor httpContextAccessor) : ICurrentTenantService
{
    public long? CompanyId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User?.FindFirst("companyId")?.Value;
            return long.TryParse(claim, out var companyId) ? companyId : null;
        }
    }
}
