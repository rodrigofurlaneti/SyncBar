using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.SetProductImage;

public sealed record SetProductImageCommand(
    long ProductId,
    string Extension,
    byte[] Content) : ICommand<string>;
