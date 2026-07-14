namespace SyncBar.Application.Features.Branches;

public sealed record BranchResponse(
    long Id,
    string Name,
    string? Cnpj,
    string? Phone,
    string? AddressCity,
    string? AddressState,
    bool IsActive);
