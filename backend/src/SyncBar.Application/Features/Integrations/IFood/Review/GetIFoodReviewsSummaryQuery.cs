using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Review;

public sealed record GetIfoodReviewsSummaryQuery(long BranchId) : IQuery<IfoodReviewSummaryResponse>;
