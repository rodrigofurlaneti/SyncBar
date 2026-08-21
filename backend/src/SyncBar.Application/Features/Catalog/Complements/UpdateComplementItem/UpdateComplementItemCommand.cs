using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.UpdateComplementItem;

public sealed record UpdateComplementItemCommand(long ComplementItemId, string Name) : ICommand;
