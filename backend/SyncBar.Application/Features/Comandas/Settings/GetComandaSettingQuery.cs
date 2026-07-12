using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Comandas.Settings;

public sealed record GetComandaSettingQuery(long BranchId) : IQuery<ComandaSettingResponse>;
