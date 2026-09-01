using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

public sealed record GetIfoodMerchantStatusQuery(long BranchId) : IQuery<IfoodMerchantStatusResponse>;
