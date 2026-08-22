using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

internal sealed class GetIFoodOperationalAlertsQueryHandler(
    IIFoodOperationalAlertStore alertStore,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodOperationalAlertsQuery, IReadOnlyCollection<IFoodOperationalAlertResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodOperationalAlertResponse>>> Handle(
        GetIFoodOperationalAlertsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodOperationalAlertsQueryHandler),
            nameof(Handle),
            null,
            (_) =>
            {
                var alerts = alertStore.GetUnacknowledged(request.CompanyId)
                    .Select(a => new IFoodOperationalAlertResponse(a.Id, a.BranchId, a.BranchName, a.Title, a.Message, a.Severity.ToString(), a.CreatedAtUtc))
                    .ToList() as IReadOnlyCollection<IFoodOperationalAlertResponse>;

                return Task.FromResult(Result.Success(alerts));
            });
    }
}
