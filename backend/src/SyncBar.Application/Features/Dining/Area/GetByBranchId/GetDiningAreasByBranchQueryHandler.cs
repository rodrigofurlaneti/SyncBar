using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Dining.Area.GetByBranchId
{
    internal sealed class GetDiningAreasByBranchQueryHandler : BaseQueryHandler<GetDiningAreasByBranchQuery, IReadOnlyCollection<DiningAreaListResponse>>
    {
        private readonly IDiningAreaRepository _diningAreaRepository;
        public GetDiningAreasByBranchQueryHandler(
            IDiningAreaRepository diningAreaRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _diningAreaRepository = diningAreaRepository;
        }
        public override Task<Result<IReadOnlyCollection<DiningAreaListResponse>>> Handle(GetDiningAreasByBranchQuery request, CancellationToken cancellationToken) =>
            ExecuteWithLogAsync(
                nameof(GetDiningAreasByBranchQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var diningAreas = await _diningAreaRepository.GetByBranchIdAsync(request.BranchId, cancellationToken);
                    IReadOnlyCollection<DiningAreaListResponse> response = diningAreas
                        .Select(d => new DiningAreaListResponse(d.Id, d.Name, d.IsActive))
                        .ToList();
                    return Result.Success(response);
                });
    }
}