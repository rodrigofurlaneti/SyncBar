using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Dining.Table.Update
{
    internal sealed class UpdateDiningAreaTableCommandHandler : BaseCommandHandler<UpdateDiningAreaTableCommand>
    {
        private readonly IDiningAreaTableRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        public UpdateDiningAreaTableCommandHandler(
            IDiningAreaTableRepository repository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }
        public override Task<Result> Handle(UpdateDiningAreaTableCommand request, CancellationToken cancellationToken) =>
            ExecuteWithLogAsync(
                nameof(UpdateDiningAreaTableCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
                    if (entity is null)
                        return Result.Failure(new Error("DiningAreaTable.NotFound", "The dining area table assignment was not found."));
                    if (entity.DiningTableId != request.DiningTableId)
                    {
                        bool exists = await _repository.ExistsByTableIdAsync(request.DiningTableId, cancellationToken);
                        if (exists)
                            return Result.Failure(new Error("DiningAreaTable.AlreadyAssigned", "This new table is already assigned to an active dining area."));
                    }
                    entity.UpdateAssignment(request.DiningAreaId, request.DiningTableId);
                    await _repository.UpdateAsync(entity, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
    }
}
