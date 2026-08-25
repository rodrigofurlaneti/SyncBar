using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Domain.Entities; // <-- ADICIONE ESTA LINHA AQUI

namespace SyncBar.Application.Features.Dining.Assignment.Create
{
    internal sealed class CreateDiningAreaAssignmentCommandHandler : BaseCommandHandler<CreateDiningAreaAssignmentCommand, long>
    {
        private readonly IDiningAreaAssignmentRepository _assignmentRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateDiningAreaAssignmentCommandHandler(
            IDiningAreaAssignmentRepository assignmentRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
        }

        public override Task<Result<long>> Handle(CreateDiningAreaAssignmentCommand request, CancellationToken cancellationToken) =>
            ExecuteWithLogAsync(
                nameof(CreateDiningAreaAssignmentCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var assignment = DiningAreaAssignment.Create(request.DiningAreaId, request.EmployeeId, request.StartAt);
                    if (assignment.IsFailure)
                        return Result.Failure<long>(assignment.Error);
                    await _assignmentRepository.AddAsync(assignment.Value, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);
                    return Result.Success(assignment.Value.Id);
                });
    }
}