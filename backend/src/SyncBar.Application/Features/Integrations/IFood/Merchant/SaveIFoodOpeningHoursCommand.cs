using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

public sealed record IFoodOpeningHourShiftInput(int DayOfWeek, string Start, int DurationMinutes);

// Sempre a lista COMPLETA de turnos ativos da filial — PUT /opening-hours no iFood substitui
// tudo de uma vez, então o SyncBar nunca envia um diff (ver comentário em IFoodOpeningHours).
public sealed record SaveIFoodOpeningHoursCommand(long BranchId, IReadOnlyCollection<IFoodOpeningHourShiftInput> Shifts) : ICommand;
