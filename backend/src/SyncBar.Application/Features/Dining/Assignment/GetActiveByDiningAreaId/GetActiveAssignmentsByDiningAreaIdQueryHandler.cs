using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Dining.Assignment.GetActiveByDiningAreaId
{
    internal sealed class GetActiveAssignmentsByDiningAreaIdQueryHandler : BaseQueryHandler<GetActiveAssignmentsByDiningAreaIdQuery, IReadOnlyCollection<DiningAreaAssignmentListResponse>>
    {
        private readonly IDiningAreaAssignmentRepository _repository;

        public GetActiveAssignmentsByDiningAreaIdQueryHandler(
            IDiningAreaAssignmentRepository repository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _repository = repository;
        }
        public override Task<Result<IReadOnlyCollection<DiningAreaAssignmentListResponse>>> Handle(GetActiveAssignmentsByDiningAreaIdQuery request, CancellationToken cancellationToken) =>
            ExecuteWithLogAsync(
                nameof(GetActiveAssignmentsByDiningAreaIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entities = await _repository.GetActiveByDiningAreaIdAsync(request.DiningAreaId, cancellationToken);
                    IReadOnlyCollection<DiningAreaAssignmentListResponse> response = entities
                        .Select(e => new DiningAreaAssignmentListResponse(e.Id, e.DiningAreaId, e.EmployeeId, e.StartAt))
                        .ToList();
                    return Result.Success(response);
                });
    }
}
