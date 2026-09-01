using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

public sealed record GetIfoodOpeningHoursQuery(long BranchId) : IQuery<IfoodOpeningHoursResponse>;
