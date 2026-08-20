using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.CreateComplementGroup;

public sealed record CreateComplementGroupCommand(
    long CompanyId,
    string Name,
    long ComplementGroupTypeId,
    int MinSelection,
    int MaxSelection) : ICommand<long>;
