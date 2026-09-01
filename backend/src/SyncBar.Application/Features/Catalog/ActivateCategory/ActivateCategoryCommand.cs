using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.ActivateCategory;

public sealed record ActivateCategoryCommand(long CategoryId) : ICommand;
