using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.UnlinkProductComplementGroup;

public sealed record UnlinkProductComplementGroupCommand(long ProductComplementGroupId) : ICommand;
