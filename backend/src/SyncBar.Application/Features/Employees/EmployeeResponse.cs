namespace SyncBar.Application.Features.Employees;

public sealed record EmployeeResponse(
    long Id,
    long BranchId,
    long JobTitleId,
    string Name,
    string Cpf,
    string? Email,
    string? Phone,
    DateTime HiredAt,
    DateTime? DismissedAt,
    decimal? Salary,
    decimal? CommissionPercent,
    bool IsActive);

public sealed record JobTitleResponse(long Id, string Name);
