using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

// Leitura é só local (a cópia editável em IFoodOpeningHours) — não chama o iFood, mesma decisão
// registrada no doc de status (Fase 5): a tela edita a cópia local e sincroniza ao salvar, não
// busca o estado remoto a cada carregamento de tela.
internal sealed class GetIFoodOpeningHoursQueryHandler(
    IIFoodOpeningHoursRepository openingHoursRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodIntegrationSettingRepository settingRepository,
    IBranchRepository branchRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodOpeningHoursQuery, IFoodOpeningHoursResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodOpeningHoursResponse>> Handle(
        GetIFoodOpeningHoursQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodOpeningHoursQueryHandler),
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
                    hasCustomerId = !string.IsNullOrWhiteSpace(setting?.IFoodCustomerId);
                }

                var response = new IFoodOpeningHoursResponse(
                    shifts.Select(s => new IFoodOpeningHourShiftResponse(s.DayOfWeek, s.Start.ToString(@"hh\:mm"), s.DurationMinutes)).ToList(),
                    mapping?.PreparationTimeMinutes,
                    hasCustomerId);

                return Result.Success(response);
            });
    }
}
