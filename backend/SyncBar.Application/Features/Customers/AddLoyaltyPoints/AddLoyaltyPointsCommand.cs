using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Customers.AddLoyaltyPoints;

public sealed record AddLoyaltyPointsCommand(long CustomerId, int Points) : ICommand;
