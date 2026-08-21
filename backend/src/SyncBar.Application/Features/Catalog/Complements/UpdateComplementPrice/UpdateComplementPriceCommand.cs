using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.UpdateComplementPrice;

public sealed record UpdateComplementPriceCommand(long ComplementGroupId, long ComplementId, decimal ExtraPrice) : ICommand;
