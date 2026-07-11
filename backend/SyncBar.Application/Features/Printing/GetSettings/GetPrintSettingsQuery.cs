using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Printing.GetSettings;

public sealed record GetPrintSettingsQuery(long BranchId) : IQuery<PrintSettingsResponse>;
