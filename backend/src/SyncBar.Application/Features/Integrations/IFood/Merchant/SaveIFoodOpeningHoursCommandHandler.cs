using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using DomainIFoodOpeningHours = SyncBar.Domain.Entities.IFoodOpeningHours;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

// Sincroniza com o iFood ANTES de gravar local — se o PUT /opening-hours falhar, nada é salvo
// aqui (evita a cópia local ficar "à frente" do que o iFood realmente tem configurado). Ver
// decisão de design no doc de status (Fase 5): a tela edita a cópia local e reenvia tudo.
internal sealed class SaveIFoodOpeningHoursCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodMerchantClient merchantClient,
    IIFoodOpeningHoursRepository openingHoursRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SaveIFoodOpeningHoursCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(SaveIFoodOpeningHoursCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SaveIFoodOpeningHoursCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;

                var domainShifts = new List<DomainIFoodOpeningHours>();
                foreach (var shift in request.Shifts)
                {
                    if (!TimeSpan.TryParse(shift.Start, out var start))
                        return Result.Failure(new Error("IFoodOpeningHours.InvalidStart", $"Invalid start time: {shift.Start}"));

                    var created = DomainIFoodOpeningHours.Create(request.BranchId, shift.DayOfWeek, start, shift.DurationMinutes);
                    if (created.IsFailure)
                        return Result.Failure(created.Error);

                    domainShifts.Add(created.Value);
                }

                var remoteShifts = domainShifts
                    .Select(s => new IFoodOpeningHourShift(s.DayOfWeek, s.Start, s.DurationMinutes))
                    .ToList();

                var syncResult = await merchantClient.SetOpeningHoursAsync(token, merchantId, remoteShifts, cancellationToken);
                if (!syncResult.Success)
                    return Result.Failure(new Error("IFoodMerchant.SyncOpeningHoursFailed", syncResult.ErrorMessage ?? "Failed to sync opening hours with iFood."));

                // Só grava local depois do iFood confirmar — substitui a lista inteira (soft
                // delete dos turnos atuais + insere os novos), mesma semântica do PUT remoto.
                var current = await openingHoursRepository.GetByBranchForUpdateAsync(request.BranchId, cancellationToken);
                foreach (var existing in current)
                    existing.Deactivate();

                await openingHoursRepository.AddRangeAsync(domainShifts, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return Result.Success();
            });
    }
}
