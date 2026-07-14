namespace SyncBar.Application.Features.Customers;

public sealed record CustomerResponse(
    long Id,
    string Name,
    string? Phone,
    string? Cpf,
    string? Email,
    int LoyaltyPoints,
    bool IsActive);
