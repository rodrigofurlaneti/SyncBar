using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Security;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using DomainIfoodSetting = SyncBar.Domain.Entities.IfoodIntegrationSetting;

namespace SyncBar.Application.Features.Integrations.Ifood;

internal sealed class SaveIfoodSettingsCommandHandler : BaseCommandHandler<SaveIfoodSettingsCommand>
{
    // Purpose fixo — trocar essa string quebra a descriptografia de segredos já salvos.
    private const string ProtectorPurpose = "SyncBar.Integrations.Ifood.ClientSecret.v1";

    private readonly IIfoodIntegrationSettingRepository _settingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecretProtector _secretProtector;

    public SaveIfoodSettingsCommandHandler(
        IIfoodIntegrationSettingRepository settingRepository,
        ISecretProtector secretProtector,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _settingRepository = settingRepository;
        _unitOfWork = unitOfWork;
        _secretProtector = secretProtector;
    }

    public override async Task<Result> Handle(SaveIfoodSettingsCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SaveIfoodSettingsCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                var encryptedSecret = EncryptClientSecret(request.ClientSecret);
                var IfoodCustomerId = NormalizeCustomerId(request.IfoodCustomerId);

                var upsertResult = await UpsertSettingAsync(request, encryptedSecret, IfoodCustomerId, cancellationToken);
                if (upsertResult.IsFailure)
                    return upsertResult;

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }

    // Upsert por empresa — mesmo padrão do ServiceFeeSetting/ComandaSetting, só que
    // por CompanyId em vez de BranchId (o app do Ifood é centralizado por empresa).
    private async Task<Result> UpsertSettingAsync(
        SaveIfoodSettingsCommand request,
        string? encryptedSecret,
        string? IfoodCustomerId,
        CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetByCompanyForUpdateAsync(request.CompanyId, cancellationToken);

        if (setting is not null)
            return setting.SaveCredentials(request.ClientId, encryptedSecret, request.Enabled, IfoodCustomerId);

        var created = DomainIfoodSetting.Create(request.CompanyId);
        if (created.IsFailure)
            return Result.Failure(created.Error);

        var saved = created.Value.SaveCredentials(request.ClientId, encryptedSecret, request.Enabled, IfoodCustomerId);
        if (saved.IsFailure)
            return saved;

        await _settingRepository.AddAsync(created.Value, cancellationToken);
        return Result.Success();
    }

    private string? EncryptClientSecret(string? clientSecret)
        => string.IsNullOrWhiteSpace(clientSecret)
            ? null
            : _secretProtector.Protect(ProtectorPurpose, clientSecret);

    private static string? NormalizeCustomerId(string? IfoodCustomerId)
        => string.IsNullOrWhiteSpace(IfoodCustomerId) ? null : IfoodCustomerId.Trim();
}
