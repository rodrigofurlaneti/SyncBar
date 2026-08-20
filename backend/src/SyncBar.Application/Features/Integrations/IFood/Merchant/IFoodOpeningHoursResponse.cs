namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

// Start em "HH:mm" (string) — mais simples pro frontend consumir direto num <input type="time">
// sem lidar com o formato de TimeSpan serializado em JSON.
public sealed record IFoodOpeningHourShiftResponse(int DayOfWeek, string Start, int DurationMinutes);

public sealed record IFoodOpeningHoursResponse(
    IReadOnlyCollection<IFoodOpeningHourShiftResponse> Shifts, int? PreparationTimeMinutes, bool HasIFoodCustomerId);
