using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.GetCategoryById;

public sealed record GetCategoryByIdQuery(long CategoryId) : IQuery<CategoryResponse>;
