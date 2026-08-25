using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Dining.Area.Create
{
    internal sealed class CreateDiningAreaCommandHandler : BaseCommandHandler<CreateDiningAreaCommand, long>
    {
        private readonly IDiningAreaRepository _diningAreaRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateDiningAreaCommandHandler(
            IDiningAreaRepository diningAreaRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _diningAreaRepository = diningAreaRepository;
            _unitOfWork = unitOfWork;
        }

        public override Task<Result<long>> Handle(CreateDiningAreaCommand request, CancellationToken cancellationToken) =>
            ExecuteWithLogAsync(
                nameof(CreateDiningAreaCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var diningArea = DiningArea.Create(request.BranchId, request.Name);

                    if (diningArea.IsFailure)
                        return Result.Failure<long>(diningArea.Error);

                    await _diningAreaRepository.AddAsync(diningArea.Value, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success(diningArea.Value.Id);
                });
    }
}
