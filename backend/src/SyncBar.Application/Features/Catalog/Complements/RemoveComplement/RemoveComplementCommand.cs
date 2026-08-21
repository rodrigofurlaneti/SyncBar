using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.RemoveComplement;

public sealed record RemoveComplementCommand(long ComplementGroupId, long ComplementId) : ICommand;
