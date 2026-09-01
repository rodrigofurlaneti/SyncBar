namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

public sealed record IfoodOperationalAlertResponse(
    Guid Id,
    long BranchId,
    string BranchName,
    string Title,
    string Message,
    string Severity, // "Info" | "Warning" | "Critical" — enum serializado como string, ver IfoodOperationalAlertSeverity
    DateTime CreatedAtUtc);
