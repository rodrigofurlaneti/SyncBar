using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.DeactivateComplementGroup;

public sealed record DeactivateComplementGroupCommand(long ComplementGroupId) : ICommand;
