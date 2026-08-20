using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.CreateComplementItem;

public sealed record CreateComplementItemCommand(long CompanyId, string Name) : ICommand<long>;
