using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Dining.Table.GetByDiningAreaId
{
    internal sealed class GetDiningAreaTablesByAreaIdQueryHandler : BaseQueryHandler<GetDiningAreaTablesByAreaIdQuery, IReadOnlyCollection<DiningAreaTableListResponse>>
    {
        private readonly IDiningAreaTableRepository _repository;
        public GetDiningAreaTablesByAreaIdQueryHandler(
            IDiningAreaTableRepository repository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _repository = repository;
        }
        public override Task<Result<IReadOnlyCollection<DiningAreaTableListResponse>>> Handle(GetDiningAreaTablesByAreaIdQuery request, CancellationToken cancellationToken) =>
            ExecuteWithLogAsync(
                nameof(GetDiningAreaTablesByAreaIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entities = await _repository.GetByDiningAreaIdAsync(request.DiningAreaId, cancellationToken);
                    IReadOnlyCollection<DiningAreaTableListResponse> response = entities
                        .Select(e => new DiningAreaTableListResponse(e.Id, e.DiningTableId, e.IsActive))
                        .ToList();
                    return Result.Success(response);
                });
    }
}
