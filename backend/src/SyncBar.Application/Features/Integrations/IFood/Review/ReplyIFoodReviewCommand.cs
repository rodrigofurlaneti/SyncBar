using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Review;

public sealed record ReplyIfoodReviewCommand(long BranchId, string ReviewId, string Text) : ICommand<IfoodReviewReplyResponse>;
