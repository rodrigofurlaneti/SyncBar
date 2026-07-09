using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.DeactivateProduct;

public sealed record DeactivateProductCommand(long ProductId) : ICommand;
