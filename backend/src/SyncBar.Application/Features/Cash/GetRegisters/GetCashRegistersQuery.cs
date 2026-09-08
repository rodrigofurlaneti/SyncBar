using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.GetRegisters;

public sealed record GetCashRegistersQuery(long BranchId) : IQuery<IReadOnlyCollection<CashRegisterResponse>>;
public sealed record CashRegisterResponse(long Id, string Name);
internal sealed class GetCashRegistersQueryHandler(ICashRegisterRepository registers,
    ILogTrackerRepository logs, IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetCashRegistersQuery, IReadOnlyCollection<CashRegisterResponse>>(logs, unitOfWork)
{
    public override Task<Result<IReadOnlyCollection<CashRegisterResponse>>> Handle(GetCashRegistersQuery request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetCashRegistersQueryHandler), nameof(Handle), null, async _ =>
        {
            var items = await registers.GetByBranchAsync(request.BranchId, ct);
            return Result.Success<IReadOnlyCollection<CashRegisterResponse>>(items.OrderBy(r => r.Id)
                .Select(r => new CashRegisterResponse(r.Id, r.Name)).ToList());
        });
}
