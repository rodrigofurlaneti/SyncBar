using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

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
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodOpeningHoursQuery, IfoodOpeningHoursResponse>(logRepository, unitOfWork)
{
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
                }

                var response = new IfoodOpeningHoursResponse(
                    shifts.Select(s => new IfoodOpeningHourShiftResponse(s.DayOfWeek, s.Start.ToString(@"hh\:mm"), s.DurationMinutes)).ToList(),
                    mapping?.PreparationTimeMinutes,
                    hasCustomerId);

                return Result.Success(response);
            });
    }
}
