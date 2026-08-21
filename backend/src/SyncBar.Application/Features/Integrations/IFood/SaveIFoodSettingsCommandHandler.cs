using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Security;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using DomainIFoodSetting = SyncBar.Domain.Entities.IFoodIntegrationSetting;

namespace SyncBar.Application.Features.Integrations.IFood;

internal sealed class SaveIFoodSettingsCommandHandler : BaseCommandHandler<SaveIFoodSettingsCommand>
{
    // Purpose fixo — trocar essa string quebra a descriptografia de segredos já salvos.
    private const string ProtectorPurpose = "SyncBar.Integrations.IFood.ClientSecret.v1";

    private readonly IIFoodIntegrationSettingRepository _settingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecretProtector _secretProtector;

    public SaveIFoodSettingsCommandHandler(
        IIFoodIntegrationSettingRepository settingRepository,
        ISecretProtector secretProtector,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _settingRepository = settingRepository;
        _unitOfWork = unitOfWork;
        _secretProtector = secretProtector;
    }

    public override async Task<Result> Handle(SaveIFoodSettingsCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SaveIFoodSettingsCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                var encryptedSecret = string.IsNullOrWhiteSpace(request.ClientSecret)
                    ? null
                    : _secretProtector.Protect(ProtectorPurpose, request.ClientSecret);
                var ifoodCustomerId = string.IsNullOrWhiteSpace(request.IFoodCustomerId) ? null : request.IFoodCustomerId.Trim();

                // Upsert por empresa — mesmo padrão do ServiceFeeSetting/ComandaSetting, só que
                // por CompanyId em vez de BranchId (o app do iFood é centralizado por empresa).
                var setting = await _settingRepository.GetByCompanyForUpdateAsync(request.CompanyId, cancellationToken);
                if (setting is null)
                {
                    var created = DomainIFoodSetting.Create(request.CompanyId);
                    if (created.IsFailure)
                        return Result.Failure(created.Error);

                    var saved = created.Value.SaveCredentials(request.ClientId, encryptedSecret, request.Enabled, ifoodCustomerId);
                    if (saved.IsFailure)
                        return saved;

                    await _settingRepository.AddAsync(created.Value, cancellationToken);
                }
                else
                {
                    var saved = setting.SaveCredentials(request.ClientId, encryptedSecret, request.Enabled, ifoodCustomerId);
                    if (saved.IsFailure)
                        return saved;
                }

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}
