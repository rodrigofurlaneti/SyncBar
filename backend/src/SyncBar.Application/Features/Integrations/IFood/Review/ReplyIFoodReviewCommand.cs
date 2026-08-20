using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Review;

public sealed record ReplyIFoodReviewCommand(long BranchId, string ReviewId, string Text) : ICommand<IFoodReviewReplyResponse>;
