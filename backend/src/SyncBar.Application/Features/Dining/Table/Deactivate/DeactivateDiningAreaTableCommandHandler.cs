using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Dining.Table.Deactivate
{
    internal sealed class DeactivateDiningAreaTableCommandHandler : BaseCommandHandler<DeactivateDiningAreaTableCommand>
    {
        private readonly IDiningAreaTableRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        public DeactivateDiningAreaTableCommandHandler(
            IDiningAreaTableRepository repository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }
        public override Task<Result> Handle(DeactivateDiningAreaTableCommand request, CancellationToken cancellationToken) =>
            ExecuteWithLogAsync(
                nameof(DeactivateDiningAreaTableCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
                    if (entity is null)
                        return Result.Failure(new Error("DiningAreaTable.NotFound", "The dining area table assignment was not found."));
                    entity.Deactivate();
                    await _repository.UpdateAsync(entity, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);
                    return Result.Success();
                });
    }
}
