using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.LinkProductComplementGroup;

public sealed record LinkProductComplementGroupCommand(long ProductId, long ComplementGroupId, int DisplayOrder) : ICommand<long>;
