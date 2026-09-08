using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Application.Abstractions.Integrations.Ifood;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

// Leitura é só local (a cópia editável em IfoodOpeningHours) — não chama o Ifood, mesma decisão
// registrada no doc de status (Fase 5): a tela edita a cópia local e sincroniza ao salvar, não
// busca o estado remoto a cada carregamento de tela.
internal sealed class GetIfoodOpeningHoursQueryHandler(
    IIfoodOpeningHoursRepository openingHoursRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodIntegrationSettingRepository settingRepository,
    IBranchRepository branchRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork,
    IIfoodTokenProvider tokenProvider,
    IIfoodMerchantClient merchantClient)
    : BaseQueryHandler<GetIfoodOpeningHoursQuery, IfoodOpeningHoursResponse>(logRepository, unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public override async Task<Result<IfoodOpeningHoursResponse>> Handle(
        GetIfoodOpeningHoursQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodOpeningHoursQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var shifts = await openingHoursRepository.GetByBranchAsync(request.BranchId, cancellationToken);
                var mapping = await mappingRepository.GetByBranchAsync(request.BranchId, cancellationToken);

                var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
                var hasCustomerId = false;
                if (branch is not null)
                {
                    var setting = await settingRepository.GetByCompanyAsync(branch.CompanyId, cancellationToken);
                    hasCustomerId = !string.IsNullOrWhiteSpace(setting?.IfoodCustomerId);
                    if (mapping?.IsActive == true && !string.IsNullOrWhiteSpace(mapping.MerchantId))
                    {
                        var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                        if (string.IsNullOrWhiteSpace(token))
                            return Result.Failure<IfoodOpeningHoursResponse>(new Error("IfoodMerchant.NoToken", "Não foi possível autenticar com o iFood."));
                        var remote = await merchantClient.GetOpeningHoursAsync(token, mapping.MerchantId, cancellationToken);
                        if (!remote.Success)
                            return Result.Failure<IfoodOpeningHoursResponse>(new Error("IfoodMerchant.ReadHoursFailed", remote.ErrorMessage ?? "Falha ao consultar horários no iFood."));
                        var replacements = new List<SyncBar.Domain.Entities.IfoodOpeningHours>();
                        foreach (var shift in remote.Shifts)
                        {
                            var created = SyncBar.Domain.Entities.IfoodOpeningHours.Create(request.BranchId, shift.DayOfWeek, shift.Start, shift.DurationMinutes);
                            if (created.IsFailure) return Result.Failure<IfoodOpeningHoursResponse>(created.Error);
                            replacements.Add(created.Value);
                        }
                        var prep = hasCustomerId
                            ? await merchantClient.GetPreparationTimeAsync(token, mapping.MerchantId, setting!.IfoodCustomerId!, cancellationToken)
                            : null;
                        if (prep?.Success == false)
                            return Result.Failure<IfoodOpeningHoursResponse>(new Error("IfoodMerchant.ReadPreparationFailed", prep.ErrorMessage!));
                        foreach (var old in await openingHoursRepository.GetByBranchForUpdateAsync(request.BranchId, cancellationToken)) old.Deactivate();
                        await openingHoursRepository.AddRangeAsync(replacements, cancellationToken);
                        if (prep is not null)
                        {
                            var trackedMapping = await mappingRepository.GetByBranchForUpdateAsync(request.BranchId, cancellationToken);
                            trackedMapping?.SetPreparationTime(prep.Minutes);
                            mapping.SetPreparationTime(prep.Minutes);
                        }
                        await _unitOfWork.CommitAsync(cancellationToken);
                        shifts = replacements;
                    }
                }

                var response = new IfoodOpeningHoursResponse(
                    shifts.Select(s => new IfoodOpeningHourShiftResponse(s.DayOfWeek, s.Start.ToString(@"hh\:mm"), s.DurationMinutes)).ToList(),
                    mapping?.PreparationTimeMinutes,
                    hasCustomerId);

                return Result.Success(response);
            });
    }
}
