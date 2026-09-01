using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.GetTableReadingValidationSetting;

public sealed record GetTableReadingValidationSettingQuery(long BranchId) : IQuery<TableReadingValidationSettingResponse>;
