using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.OrderOrigin.ExistsByName
{
    internal sealed class ExistsOrderOriginByNameQueryHandler : BaseQueryHandler<ExistsOrderOriginByNameQuery, bool>
    {
        private readonly IOrderOriginRepository _orderOriginRepository;

        public ExistsOrderOriginByNameQueryHandler(
            IOrderOriginRepository orderOriginRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderOriginRepository = orderOriginRepository;
        }

        public override async Task<Result<bool>> Handle(
            ExistsOrderOriginByNameQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ExistsOrderOriginByNameQueryHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    var exists = await _orderOriginRepository.ExistsByNameAsync(
                        request.CompanyId,
                        request.BranchId,
                        request.Name,
                        request.ExcludeId,
                        cancellationToken);

                    return Result.Success(exists);
                });
        }
    }
}
