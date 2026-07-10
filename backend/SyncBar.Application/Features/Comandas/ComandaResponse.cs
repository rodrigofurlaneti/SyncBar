namespace SyncBar.Application.Features.Comandas;

public sealed record ComandaResponse(long Id, long BranchId, long ComandaStatusId, string Code);
