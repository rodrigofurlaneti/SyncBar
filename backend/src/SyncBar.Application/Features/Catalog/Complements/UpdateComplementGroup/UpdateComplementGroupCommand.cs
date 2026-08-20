using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.UpdateComplementGroup;

public sealed record UpdateComplementGroupCommand(
    long ComplementGroupId,
    string Name,
    long ComplementGroupTypeId,
    int MinSelection,
    int MaxSelection) : ICommand;
