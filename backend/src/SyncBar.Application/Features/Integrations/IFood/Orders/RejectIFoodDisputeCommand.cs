using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

// Fase 9b — disputas Handshake (POST disputes/{disputeId}/reject). Ver ressalva em
// AcceptIFoodDisputeCommand.
public sealed record RejectIFoodDisputeCommand(long BranchId, string DisputeId, string Reason) : ICommand<IFoodDisputeActionResponse>;
