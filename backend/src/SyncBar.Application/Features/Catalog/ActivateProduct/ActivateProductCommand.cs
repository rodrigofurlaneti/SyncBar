using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.ActivateProduct;

public sealed record ActivateProductCommand(long ProductId) : ICommand;
