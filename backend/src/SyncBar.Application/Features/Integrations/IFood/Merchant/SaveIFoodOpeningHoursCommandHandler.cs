using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using DomainIfoodOpeningHours = SyncBar.Domain.Entities.IfoodOpeningHours;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

// Sincroniza com o Ifood ANTES de gravar local — se o PUT /opening-hours falhar, nada é salvo
// aqui (evita a cópia local ficar "à frente" do que o Ifood realmente tem configurado). Ver
// decisão de design no doc de status (Fase 5): a tela edita a cópia local e reenvia tudo.
internal sealed class SaveIfoodOpeningHoursCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodMerchantClient merchantClient,
    IIfoodOpeningHoursRepository openingHoursRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SaveIfoodOpeningHoursCommand>(logRepository, unitOfWork)
{
    // Campo explícito: capturar o parâmetro primário que também vai para a base dispara CS9107.
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override async Task<Result> Handle(SaveIfoodOpeningHoursCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SaveIfoodOpeningHoursCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;

                var domainShifts = new List<DomainIfoodOpeningHours>();
                foreach (var shift in request.Shifts)
                {
                    if (!TimeSpan.TryParse(shift.Start, out var start))
                        return Result.Failure(new Error("IfoodOpeningHours.InvalidStart", $"Invalid start time: {shift.Start}"));

                    var created = DomainIfoodOpeningHours.Create(request.BranchId, shift.DayOfWeek, start, shift.DurationMinutes);
                    if (created.IsFailure)
                        return Result.Failure(created.Error);

                    domainShifts.Add(created.Value);
                }

                var remoteShifts = domainShifts
                    .Select(s => new IfoodOpeningHourShift(s.DayOfWeek, s.Start, s.DurationMinutes))
                    .ToList();

                var syncResult = await merchantClient.SetOpeningHoursAsync(token, merchantId, remoteShifts, cancellationToken);
                if (!syncResult.Success)
                    return Result.Failure(new Error("IfoodMerchant.SyncOpeningHoursFailed", syncResult.ErrorMessage ?? "Failed to sync opening hours with Ifood."));

                // Só grava local depois do Ifood confirmar — substitui a lista inteira (soft
                // delete dos turnos atuais + insere os novos), mesma semântica do PUT remoto.
                var current = await openingHoursRepository.GetByBranchForUpdateAsync(request.BranchId, cancellationToken);
                foreach (var existing in current)
                    existing.Deactivate();

                await openingHoursRepository.AddRangeAsync(domainShifts, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success();
            });
    }
}
