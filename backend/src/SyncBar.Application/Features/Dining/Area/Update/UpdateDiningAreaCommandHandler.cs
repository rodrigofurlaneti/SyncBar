using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Dining.Area.Update
{
    internal sealed class UpdateDiningAreaCommandHandler : BaseCommandHandler<UpdateDiningAreaCommand>
    {
        private readonly IDiningAreaRepository _diningAreaRepository;
        private readonly IUnitOfWork _unitOfWork;
        public UpdateDiningAreaCommandHandler(
            IDiningAreaRepository diningAreaRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _diningAreaRepository = diningAreaRepository;
            _unitOfWork = unitOfWork;
        }
        public override Task<Result> Handle(UpdateDiningAreaCommand request, CancellationToken cancellationToken) =>
            ExecuteWithLogAsync(
                nameof(UpdateDiningAreaCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var diningArea = await _diningAreaRepository.GetByIdAsync(request.Id, cancellationToken);
                    if (diningArea is null)
                        return Result.Failure(new Error("DiningArea.NotFound", "The dining area was not found."));
                    diningArea.UpdateName(request.Name);
                    await _diningAreaRepository.UpdateAsync(diningArea, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);
                    return Result.Success();
                });
    }
}
