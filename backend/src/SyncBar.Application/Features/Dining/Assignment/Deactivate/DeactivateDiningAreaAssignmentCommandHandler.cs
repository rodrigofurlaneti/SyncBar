using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Dining.Assignment.Deactivate
{
    internal sealed class DeactivateDiningAreaAssignmentCommandHandler : BaseCommandHandler<DeactivateDiningAreaAssignmentCommand>
    {
        private readonly IDiningAreaAssignmentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        public DeactivateDiningAreaAssignmentCommandHandler(
            IDiningAreaAssignmentRepository repository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }
        public override Task<Result> Handle(DeactivateDiningAreaAssignmentCommand request, CancellationToken cancellationToken) =>
            ExecuteWithLogAsync(
                nameof(DeactivateDiningAreaAssignmentCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
                    if (entity is null)
                        return Result.Failure(new Error("DiningAreaAssignment.NotFound", "The dining area assignment was not found."));
                    entity.Deactivate();
                    await _repository.UpdateAsync(entity, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);
                    return Result.Success();
                });
    }
}
