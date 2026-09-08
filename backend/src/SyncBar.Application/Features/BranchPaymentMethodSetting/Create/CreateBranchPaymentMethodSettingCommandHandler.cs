using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.Create
{
    internal sealed class CreateBranchPaymentMethodSettingCommandHandler
        : BaseCommandHandler<CreateBranchPaymentMethodSettingCommand, CreateBranchPaymentMethodSettingResponse>
    {
        private readonly IBranchPaymentMethodSettingRepository _settingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateBranchPaymentMethodSettingCommandHandler(
            IBranchPaymentMethodSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<CreateBranchPaymentMethodSettingResponse>> Handle(
            CreateBranchPaymentMethodSettingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(CreateBranchPaymentMethodSettingCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // 1. Evita duplicidade de configuração para o mesmo escopo (Empresa ou Filial específica)
                    var existingSetting = await _settingRepository.GetByScopeAsync(
                        request.CompanyId,
                        request.BranchId,
                        cancellationToken);

                    if (existingSetting is not null)
                    {
                        var scopeMsg = request.BranchId.HasValue
                            ? $"para a filial {request.BranchId.Value}"
                            : $"global para a empresa {request.CompanyId}";

                        return Result.Failure<CreateBranchPaymentMethodSettingResponse>(
                            Error.Conflict(
                                "BranchPaymentMethodSetting.AlreadyExists",
                                $"Já existe uma configuração de métodos de pagamento cadastrada {scopeMsg}."));
                    }

                    // 2. Cria a entidade no Domínio
                    var settingResult = Domain.Entities.BranchPaymentMethodSetting.Create(
                        request.CompanyId,
                        request.BranchId,
                        request.EnablePix,
                        request.EnableBoleto,
                        request.EnableCreditCard,
                        request.EnableDebitCard,
                        request.EnableCashMachine,
                        request.IsActive);

                    if (settingResult.IsFailure)
                        return Result.Failure<CreateBranchPaymentMethodSettingResponse>(settingResult.Error);

                    var setting = settingResult.Value;

                    // 3. Persistência
                    await _settingRepository.AddAsync(setting, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    var response = new CreateBranchPaymentMethodSettingResponse(
                        setting.Id,
                        setting.CompanyId,
                        setting.BranchId,
                        setting.EnablePix,
                        setting.EnableBoleto,
                        setting.EnableCreditCard,
                        setting.EnableDebitCard,
                        setting.EnableCashMachine,
                        setting.IsActive, setting.CreatedAt, setting.UpdatedAt);

                    return Result.Success(response);
                });
        }
    }
}
