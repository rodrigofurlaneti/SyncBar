using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.CreateCategory;

public sealed record CreateCategoryCommand(long CompanyId, string Name, int DisplayOrder) : ICommand<long>;
