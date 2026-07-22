using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Branches.Create;

public sealed record CreateBranchCommand(
    long CompanyId,
    string Name,
    string? Cnpj,
    string? Phone,
    string? AddressStreet,
    string? AddressNumber,
    string? AddressDistrict,
    string? AddressCity,
    string? AddressState,
    string? AddressZipCode) : ICommand<long>;
