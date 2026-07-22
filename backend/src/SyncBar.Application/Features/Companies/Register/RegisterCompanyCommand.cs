using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Companies.Register;

// Onboarding self-service: cria a empresa, a primeira filial e o usuário administrador
// em uma única transação — sem isso, hoje só é possível cadastrar um cliente novo via SQL manual.
public sealed record RegisterCompanyCommand(
    string LegalName,
    string TradeName,
    string Cnpj,
    string? CompanyEmail,
    string? CompanyPhone,
    string BranchName,
    string? BranchCnpj,
    string? AddressStreet,
    string? AddressNumber,
    string? AddressDistrict,
    string? AddressCity,
    string? AddressState,
    string? AddressZipCode,
    string AdminUserName,
    string AdminEmail,
    string AdminPassword) : ICommand<RegisterCompanyResponse>;

public sealed record RegisterCompanyResponse(long CompanyId, long BranchId, long AdminUserId);
