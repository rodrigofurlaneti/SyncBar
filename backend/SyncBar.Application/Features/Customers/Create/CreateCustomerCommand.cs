using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Customers.Create;

public sealed record CreateCustomerCommand(
    long CompanyId,
    string Name,
    string? Phone,
    string? Cpf,
    string? Email) : ICommand<long>;
