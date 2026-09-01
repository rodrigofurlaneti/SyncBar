using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

public sealed record IfoodOpeningHourShiftInput(int DayOfWeek, string Start, int DurationMinutes);

// Sempre a lista COMPLETA de turnos ativos da filial — PUT /opening-hours no Ifood substitui
// tudo de uma vez, então o SyncBar nunca envia um diff (ver comentário em IfoodOpeningHours).
public sealed record SaveIfoodOpeningHoursCommand(long BranchId, IReadOnlyCollection<IfoodOpeningHourShiftInput> Shifts) : ICommand;
