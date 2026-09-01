using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.DeactivateCategory;

public sealed record DeactivateCategoryCommand(long CategoryId) : ICommand;
