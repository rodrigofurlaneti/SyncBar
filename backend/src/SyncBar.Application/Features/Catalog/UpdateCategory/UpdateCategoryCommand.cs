using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.UpdateCategory;

public sealed record UpdateCategoryCommand(long CategoryId, string Name, int DisplayOrder) : ICommand;
