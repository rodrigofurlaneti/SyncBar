using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.DeactivateComplementItem;

public sealed record DeactivateComplementItemCommand(long ComplementItemId) : ICommand;
