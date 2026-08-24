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
                var encryptedSecret = EncryptClientSecret(request.ClientSecret);
                var ifoodCustomerId = NormalizeCustomerId(request.IFoodCustomerId);

                var upsertResult = await UpsertSettingAsync(request, encryptedSecret, ifoodCustomerId, cancellationToken);
                if (upsertResult.IsFailure)
                    return upsertResult;

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }

    // Upsert por empresa — mesmo padrão do ServiceFeeSetting/ComandaSetting, só que
    // por CompanyId em vez de BranchId (o app do iFood é centralizado por empresa).
    private async Task<Result> UpsertSettingAsync(
        SaveIFoodSettingsCommand request,
        string? encryptedSecret,
        string? ifoodCustomerId,
        CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetByCompanyForUpdateAsync(request.CompanyId, cancellationToken);

        if (setting is not null)
            return setting.SaveCredentials(request.ClientId, encryptedSecret, request.Enabled, ifoodCustomerId);

        var created = DomainIFoodSetting.Create(request.CompanyId);
        if (created.IsFailure)
            return Result.Failure(created.Error);

        var saved = created.Value.SaveCredentials(request.ClientId, encryptedSecret, request.Enabled, ifoodCustomerId);
        if (saved.IsFailure)
            return saved;

        await _settingRepository.AddAsync(created.Value, cancellationToken);
        return Result.Success();
    }

    private string? EncryptClientSecret(string? clientSecret)
        => string.IsNullOrWhiteSpace(clientSecret)
            ? null
            : _secretProtector.Protect(ProtectorPurpose, clientSecret);

    private static string? NormalizeCustomerId(string? ifoodCustomerId)
        => string.IsNullOrWhiteSpace(ifoodCustomerId) ? null : ifoodCustomerId.Trim();
}
