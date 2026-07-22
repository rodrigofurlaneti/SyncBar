using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.ServiceFeeSetting;

public sealed record GetServiceFeeSettingQuery(long BranchId) : IQuery<ServiceFeeSettingResponse>;
