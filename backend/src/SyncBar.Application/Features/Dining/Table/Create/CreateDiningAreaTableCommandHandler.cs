using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Domain.Entities; 
namespace SyncBar.Application.Features.Dining.Table.Create
{
    internal sealed class CreateDiningAreaTableCommandHandler : BaseCommandHandler<CreateDiningAreaTableCommand, long>
    {
        private readonly IDiningAreaTableRepository _diningAreaTableRepository;
        private readonly IUnitOfWork _unitOfWork;
        public CreateDiningAreaTableCommandHandler(
            IDiningAreaTableRepository diningAreaTableRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _diningAreaTableRepository = diningAreaTableRepository;
            _unitOfWork = unitOfWork;
        }
        public override Task<Result<long>> Handle(CreateDiningAreaTableCommand request, CancellationToken cancellationToken) =>
            ExecuteWithLogAsync(
                nameof(CreateDiningAreaTableCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    bool exists = await _diningAreaTableRepository.ExistsByTableIdAsync(request.DiningTableId, cancellationToken);
                    if (exists)
                        return Result.Failure<long>(new Error("DiningAreaTable.AlreadyAssigned", "This table is already assigned to a dining area."));
                    var diningAreaTable = DiningAreaTable.Create(request.DiningAreaId, request.DiningTableId);
                    if (diningAreaTable.IsFailure)
                        return Result.Failure<long>(diningAreaTable.Error);
                    await _diningAreaTableRepository.AddAsync(diningAreaTable.Value, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);
                    return Result.Success(diningAreaTable.Value.Id);
                });
    }
}