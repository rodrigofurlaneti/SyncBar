namespace SyncBar.Application.Features.Suppliers;

public sealed record SupplierResponse(
    long Id,
    string LegalName,
    string? TradeName,
    string? Cnpj,
    string? Email,
    string? Phone,
    bool IsActive);
