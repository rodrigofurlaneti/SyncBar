using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.Update
{
    internal sealed class UpdateBranchPaymentMethodSettingCommandHandler
        : BaseCommandHandler<UpdateBranchPaymentMethodSettingCommand>
    {
        private readonly IBranchPaymentMethodSettingRepository _settingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateBranchPaymentMethodSettingCommandHandler(
            IBranchPaymentMethodSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            UpdateBranchPaymentMethodSettingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateBranchPaymentMethodSettingCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // Busca a entidade rastreada para atualização usando o método ForUpdate do repositório
                    var setting = await _settingRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

                    if (setting is null || setting.CompanyId != request.CompanyId)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "BranchPaymentMethodSetting.NotFound",
                                $"Configuração de métodos de pagamento com ID {request.Id} não foi encontrada para esta empresa."));
                    }

                    // Aplica as alterações utilizando o método de domínio encapsulado na entidade
                    var updateResult = setting.UpdateDetails(
                        request.EnablePix,
                        request.EnableBoleto,
                        request.EnableCreditCard,
                        request.EnableDebitCard,
                        request.EnableCashMachine,
                        request.IsActive);

                    if (updateResult.IsFailure)
                        return Result.Failure(updateResult.Error);

                    _settingRepository.Update(setting);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}