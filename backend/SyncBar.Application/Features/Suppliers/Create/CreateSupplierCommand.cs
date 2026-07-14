using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Suppliers.Create;

public sealed record CreateSupplierCommand(
    long CompanyId,
    string LegalName,
    string? TradeName,
    string? Cnpj,
    string? Email,
    string? Phone) : ICommand<long>;
