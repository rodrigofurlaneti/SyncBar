using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Dining.Area.GetById
{
    internal sealed class GetDiningAreaByIdQueryHandler : BaseQueryHandler<GetDiningAreaByIdQuery, DiningAreaResponse>
    {
        private readonly IDiningAreaRepository _diningAreaRepository;

        public GetDiningAreaByIdQueryHandler(
            IDiningAreaRepository diningAreaRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _diningAreaRepository = diningAreaRepository;
        }
        public override Task<Result<DiningAreaResponse>> Handle(GetDiningAreaByIdQuery request, CancellationToken cancellationToken) =>
            ExecuteWithLogAsync(
                nameof(GetDiningAreaByIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var diningArea = await _diningAreaRepository.GetByIdAsync(request.Id, cancellationToken);
                    if (diningArea is null)
                        return Result.Failure<DiningAreaResponse>(new Error("DiningArea.NotFound", "The dining area was not found."));
                    var response = new DiningAreaResponse(
                        diningArea.Id,
                        diningArea.Name,
                        diningArea.IsActive);
                    return Result.Success(response);
                });
    }
}
