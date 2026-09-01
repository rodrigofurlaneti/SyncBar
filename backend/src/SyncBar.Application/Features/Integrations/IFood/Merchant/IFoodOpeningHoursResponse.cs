namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

// Start em "HH:mm" (string) — mais simples pro frontend consumir direto num <input type="time">
// sem lidar com o formato de TimeSpan serializado em JSON.
public sealed record IfoodOpeningHourShiftResponse(int DayOfWeek, string Start, int DurationMinutes);

public sealed record IfoodOpeningHoursResponse(
    IReadOnlyCollection<IfoodOpeningHourShiftResponse> Shifts, int? PreparationTimeMinutes, bool HasIfoodCustomerId);
