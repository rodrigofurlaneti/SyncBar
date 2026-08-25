using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Dining.Assignment.End
{
    internal sealed class EndDiningAreaAssignmentCommandHandler : BaseCommandHandler<EndDiningAreaAssignmentCommand>
    {
        private readonly IDiningAreaAssignmentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        public EndDiningAreaAssignmentCommandHandler(
            IDiningAreaAssignmentRepository repository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }
        public override Task<Result> Handle(EndDiningAreaAssignmentCommand request, CancellationToken cancellationToken) =>
            ExecuteWithLogAsync(
                nameof(EndDiningAreaAssignmentCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
                    if (entity is null)
                        return Result.Failure(new Error("DiningAreaAssignment.NotFound", "The dining area assignment was not found."));
                    entity.EndAssignment(request.EndAt);
                    await _repository.UpdateAsync(entity, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);
                    return Result.Success();
                });
    }
}