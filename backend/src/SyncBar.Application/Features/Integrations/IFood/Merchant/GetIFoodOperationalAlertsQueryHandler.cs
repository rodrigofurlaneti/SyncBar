using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

internal sealed class GetIfoodOperationalAlertsQueryHandler(
    IIfoodOperationalAlertStore alertStore,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodOperationalAlertsQuery, IReadOnlyCollection<IfoodOperationalAlertResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IfoodOperationalAlertResponse>>> Handle(
        GetIfoodOperationalAlertsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodOperationalAlertsQueryHandler),
            nameof(Handle),
            null,
            (_) =>
            {
                var alerts = alertStore.GetUnacknowledged(request.CompanyId)
                    .Select(a => new IfoodOperationalAlertResponse(a.Id, a.BranchId, a.BranchName, a.Title, a.Message, a.Severity.ToString(), a.CreatedAtUtc))
                    .ToList() as IReadOnlyCollection<IfoodOperationalAlertResponse>;

                return Task.FromResult(Result.Success(alerts));
            });
    }
}
