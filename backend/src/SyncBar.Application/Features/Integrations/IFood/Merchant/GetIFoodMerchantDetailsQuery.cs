using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

// Fase 9c — Get merchant details (GET merchant/v1.0/merchants/{id}). Por BranchId, igual ao
// restante do módulo — resolve o MerchantId da filial via IfoodMerchantResolution.
public sealed record IfoodMerchantAddressResponse(
    string? Country, string? State, string? City, string? PostalCode, string? District,
    string? Street, string? Number, double? Latitude, double? Longitude);

public sealed record IfoodMerchantDetailsResponse(
    string? Id, string? Name, string? CorporateName, string? Description, string? Type, string? Status,
    DateTime? CreatedAt, IfoodMerchantAddressResponse? Address);

public sealed record GetIfoodMerchantDetailsQuery(long BranchId) : IQuery<IfoodMerchantDetailsResponse>;
