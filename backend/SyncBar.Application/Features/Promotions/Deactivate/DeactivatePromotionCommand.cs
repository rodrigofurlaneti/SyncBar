using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Promotions.Deactivate;

public sealed record DeactivatePromotionCommand(long PromotionId) : ICommand;
