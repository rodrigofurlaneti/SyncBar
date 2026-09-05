using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Setting.Update
{
    internal sealed class UpdateAsaasIntegrationSettingCommandHandler
        : BaseCommandHandler<UpdateAsaasIntegrationSettingCommand>
    {
        private readonly IAsaasIntegrationSettingRepository _settingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateAsaasIntegrationSettingCommandHandler(
            IAsaasIntegrationSettingRepository settingRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _settingRepository = settingRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            UpdateAsaasIntegrationSettingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateAsaasIntegrationSettingCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // 1. Busca a configuração com tracking garantindo o isolamento multi-tenant por empresa
                    var setting = await _settingRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

                    if (setting is null || setting.CompanyId != request.CompanyId)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "AsaasSetting.NotFound",
                                $"Configuração de integração com ID {request.Id} não foi encontrada para esta empresa."));
                    }

                    // 2. Atualização dos dados e credenciais na entidade de domínio
                    var updateResult = setting.UpdateDetails(
                        request.ApiKey,
                        request.WebhookToken,
                        request.Environment,
                        isActive: request.IsActive);

                    if (updateResult.IsFailure)
                        return Result.Failure(updateResult.Error);

                    // 3. Persistência
                    _settingRepository.Update(setting);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
