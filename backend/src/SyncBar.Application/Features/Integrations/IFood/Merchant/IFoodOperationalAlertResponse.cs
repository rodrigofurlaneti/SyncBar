namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

public sealed record IFoodOperationalAlertResponse(
    Guid Id,
    long BranchId,
    string BranchName,
    string Title,
    string Message,
    string Severity, // "Info" | "Warning" | "Critical" — enum serializado como string, ver IFoodOperationalAlertSeverity
    DateTime CreatedAtUtc);
