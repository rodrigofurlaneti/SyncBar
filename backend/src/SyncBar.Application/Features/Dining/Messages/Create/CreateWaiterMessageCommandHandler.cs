using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Dining.Messages.Create
{
    internal sealed class CreateWaiterMessageCommandHandler : BaseCommandHandler<CreateWaiterMessageCommand, long>
    {
        private readonly IWaiterMessageRepository _messageRepository;
        private readonly IUnitOfWork _unitOfWork;
        public CreateWaiterMessageCommandHandler(
            IWaiterMessageRepository messageRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
        }
        public override async Task<Result<long>> Handle(CreateWaiterMessageCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(CreateWaiterMessageCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    userIdBox.Value = request.SenderEmployeeId;
                    var messageResult = WaiterMessage.Create(
                        request.BranchId,
                        request.SenderEmployeeId,
                        request.RecipientEmployeeId,
                        request.DiningAreaId,
                        request.Message
                    );
                    if (messageResult.IsFailure)
                    {
                        return Result.Failure<long>(messageResult.Error);
                    }
                    await _messageRepository.AddAsync(messageResult.Value, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);
                    return Result.Success(messageResult.Value.Id);
                });
        }
    }
}