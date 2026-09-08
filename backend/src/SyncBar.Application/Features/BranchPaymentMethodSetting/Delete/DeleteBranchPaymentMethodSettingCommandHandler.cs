using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.Delete
{
    internal sealed class DeleteBranchPaymentMethodSettingCommandHandler
        : BaseCommandHandler<DeleteBranchPaymentMethodSettingCommand>
    {
        private readonly IBranchPaymentMethodSettingRepository _settingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteBranchPaymentMethodSettingCommandHandler(
            IBranchPaymentMethodSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            DeleteBranchPaymentMethodSettingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(DeleteBranchPaymentMethodSettingCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var setting = await _settingRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

                    if (setting is null || setting.CompanyId != request.CompanyId)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "BranchPaymentMethodSetting.NotFound",
                                $"Configuração de métodos de pagamento com ID {request.Id} não foi encontrada para esta empresa."));
                    }

                    _settingRepository.Delete(setting);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}